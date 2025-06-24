// <copyright file="TempAssignedPalettesSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.Palettes
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Buildings;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Entities.UniversalDelegates;
    using Unity.Jobs;
    using UnityEngine.Assertions.Must;

    /// <summary>
    /// Game system to handle assigning palette to Temp object entities when appropriate.
    /// </summary>
    public partial class TempAssignedPalettesSystem : GameSystemBase
    {
        private ILog m_Log;
        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;
        private NetToolSystem m_NetToolSystem;
        private ObjectToolSystem m_ObjectToolSystem;
        private EntityQuery m_TempMeshColorQuery;
        private EntityQuery m_TempMeshColorQueryWithOwner;
        private ModificationBarrier2 m_Barrier;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;
        private PaletteInstanceManagerSystem m_PaletteInstanceManagerSystem;
        private AssignedPaletteCustomColorSystem m_AssignedPaletteCustomColorSystem;
        private ColorPainterToolSystem m_ColorPainterToolSystem;
        private PalettesUISystem m_PalettesUISystem;
        private ColorPainterUISystem m_ColorPainterUISystem;
        private PrefabBase m_PreviousPrefabBase;
        private PseudoRandomSeed m_PreviousPsuedoRandomSeed;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ObjectToolSystem = World.GetOrCreateSystemManaged<ObjectToolSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ModificationBarrier2>();
            m_SIPColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();
            m_PaletteInstanceManagerSystem = World.GetOrCreateSystemManaged<PaletteInstanceManagerSystem>();
            m_AssignedPaletteCustomColorSystem = World.GetOrCreateSystemManaged<AssignedPaletteCustomColorSystem>();
            m_ColorPainterToolSystem = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_PalettesUISystem = World.GetOrCreateSystemManaged<PalettesUISystem>();
            m_ColorPainterUISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_NetToolSystem = World.GetOrCreateSystemManaged<NetToolSystem>();

            m_ToolSystem.EventToolChanged += OnToolChanged;
            m_ToolSystem.EventPrefabChanged += OnPrefabChanged;

            m_TempMeshColorQuery = SystemAPI.QueryBuilder()
                  .WithAll<Temp, PseudoRandomSeed, MeshColor>()
                  .WithNone<Deleted, Game.Objects.Plant, Owner, AssignedPalette>()
                  .Build();

            m_TempMeshColorQueryWithOwner = SystemAPI.QueryBuilder()
                  .WithAll<Temp, PseudoRandomSeed, MeshColor>()
                  .WithNone<Deleted, Game.Objects.Plant, AssignedPalette>()
                  .Build();

            Enabled = false;
            m_Log.Info($"{nameof(TempAssignedPalettesSystem)}.{nameof(OnCreate)}");
        }


        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            EntityQuery entityQuery = m_TempMeshColorQuery;
            if (m_ToolSystem.activeTool == m_NetToolSystem || m_ToolSystem.activeTool == m_ColorPainterToolSystem)
            {
                entityQuery = m_TempMeshColorQueryWithOwner;
            }

            Entity[] selectedPalettesArray;
            if (m_ToolSystem.activeTool == m_ColorPainterToolSystem)
            {
                selectedPalettesArray = m_ColorPainterUISystem.SelectedPaletteEntities;
            }
            else
            {
                selectedPalettesArray = m_PalettesUISystem.SelectedPalettesDuringPlacement;
            }

            if (selectedPalettesArray.Length < 2 ||
               (selectedPalettesArray[0] == Entity.Null &&
                selectedPalettesArray[1] == Entity.Null &&
                selectedPalettesArray[2] == Entity.Null))
            {
                return;
            }

            NativeArray<Entity> selectedPalettePrefabEntities = new NativeArray<Entity>(selectedPalettesArray, Allocator.TempJob);
            NativeArray<Entity> paletteInstanceEntities = new NativeArray<Entity>(3, Allocator.TempJob);

            for (int i = 0; i < 3; i++)
            {
                 paletteInstanceEntities[i] = m_PaletteInstanceManagerSystem.GetOrCreatePaletteInstanceEntity(selectedPalettePrefabEntities[i]);
            }

            AssignPalettesJob assignPalettesJob = new AssignPalettesJob()
            {
                m_EditorContainerLookup = SystemAPI.GetComponentLookup<Game.Tools.EditorContainer>(isReadOnly: true),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_NetToolActive = m_ToolSystem.activeTool == m_NetToolSystem,
                m_OwnerLookup = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true),
                m_PaletteInstanceEntities = paletteInstanceEntities,
                m_PrefabEntities = selectedPalettePrefabEntities,
                buffer = m_Barrier.CreateCommandBuffer(),
                m_BuildingLookup = SystemAPI.GetComponentLookup<Building>(isReadOnly: true),
                m_FilterType = m_ColorPainterUISystem.ColorPainterFilterType,
                m_PainterToolActive = m_ToolSystem.activeTool == m_ColorPainterToolSystem,
                m_SelectionType = m_ColorPainterUISystem.CurrentSelectionType,
                m_State = m_ColorPainterToolSystem.CurrentState,
                m_RaycastEntity = m_ColorPainterToolSystem.RaycastEntity,
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(isReadOnly: true),
                m_PalettesActive = m_SIPColorFieldsSystem.ShowPaletteChoices,
                m_TempLookup = SystemAPI.GetComponentLookup<Temp>(isReadOnly: true),
            };

            Dependency = assignPalettesJob.Schedule(entityQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(Dependency);
            selectedPalettePrefabEntities.Dispose(Dependency);
            paletteInstanceEntities.Dispose(Dependency);

            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);
            if (m_ToolSystem.activeTool == m_ObjectToolSystem &&
                m_ObjectToolSystem.actualMode == ObjectToolSystem.Mode.Create &&
                entities.Length == 1 &&
                EntityManager.TryGetBuffer(entities[0], isReadOnly: true, out DynamicBuffer<MeshColor> meshColors) &&
                meshColors.Length > 0)
            {
                ColorSet colorSet = meshColors[0].m_ColorSet;
                colorSet.m_Channel1.a = 1f;
                colorSet.m_Channel2.a = 1f;
                colorSet.m_Channel0.a = 1f;
                m_PalettesUISystem.SetNoneColors(colorSet);
            }
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            OnPrefabChanged(m_ToolSystem.activePrefab);
        }

        private void OnPrefabChanged(PrefabBase prefab)
        {
            if ((m_ToolSystem.activeTool != m_NetToolSystem &&
                m_ToolSystem.activeTool != m_ColorPainterToolSystem &&
                m_ToolSystem.activeTool != m_ObjectToolSystem) ||
               !Mod.Instance.Settings.ShowPalettesOptionDuringPlacement ||
               (m_ToolSystem.activeTool == m_ObjectToolSystem &&
               (m_ObjectToolSystem.actualMode == ObjectToolSystem.Mode.Stamp ||
                m_ObjectToolSystem.actualMode == ObjectToolSystem.Mode.Move)))
            {
                Enabled = false;
                m_PalettesUISystem.ShowPaletteChooserDuringPlacement = false;
                return;
            }

            if (prefab is not null &&
                m_PrefabSystem.TryGetEntity(prefab, out Entity prefabEntity) &&
               !EntityManager.HasComponent<Game.Prefabs.PlantData>(prefabEntity) &&
                EntityManager.TryGetBuffer(prefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshes) &&
                subMeshes.Length > 0)
            {
                for (int i = 0; i < subMeshes.Length; i++)
                {
                    if (EntityManager.TryGetBuffer(subMeshes[i].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariations) &&
                        colorVariations.Length > 0)
                    {
                        Enabled = true;
                        m_PalettesUISystem.ResetNoneColors();
                        m_PalettesUISystem.UpdatePaletteChoicesDuringPlacementBinding(Mod.Instance.Settings.ResetPaletteChoicesWhenSwitchingPrefab && m_PreviousPrefabBase != prefab);
                        m_PreviousPrefabBase = prefab;
                        return;
                    }
                }
            }
            else if (m_ToolSystem.activeTool == m_ColorPainterToolSystem)
            {
                Enabled = true;
                return;
            }

            Enabled = false;
            m_PalettesUISystem.ShowPaletteChooserDuringPlacement = false;
        }

#if BURST
        [BurstCompile]
#endif
        private struct AssignPalettesJob : IJobChunk
        {
            [ReadOnly]
            public ComponentLookup<Owner> m_OwnerLookup;
            [ReadOnly]
            public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerLookup;
            public EntityCommandBuffer buffer;
            public NativeArray<Entity> m_PrefabEntities;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public bool m_NetToolActive;
            [ReadOnly]
            public NativeArray<Entity> m_PaletteInstanceEntities;
            public bool m_PainterToolActive;
            public ColorPainterToolSystem.State m_State;
            public ColorPainterUISystem.SelectionType m_SelectionType;
            public ColorPainterUISystem.FilterType m_FilterType;
            public Entity m_RaycastEntity;
            [ReadOnly]
            public ComponentLookup<Building> m_BuildingLookup;
            [ReadOnly]
            public ComponentTypeHandle<Temp> m_TempType;
            public bool m_PalettesActive;
            [ReadOnly]
            public ComponentLookup<Temp> m_TempLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                if (m_PrefabEntities.Length < 2 ||
                        m_PaletteInstanceEntities.Length < 2 ||
                       (m_PrefabEntities[0] == Entity.Null &&
                        m_PrefabEntities[1] == Entity.Null &&
                        m_PrefabEntities[2] == Entity.Null))
                {
                    return;
                }

                for (int i = 0; i < chunk.Count; i++)
                {
                    Temp temp = tempNativeArray[i];
                    Entity instanceEntity = entityNativeArray[i];
                    if (m_NetToolActive &&
                       (!m_OwnerLookup.TryGetComponent(instanceEntity, out Owner owner) ||
                        !m_EditorContainerLookup.HasComponent(owner.m_Owner)))
                    {
                        continue;
                    }

                    // This section is necessary to fix the temp components with netlanes.
                    if (temp.m_Original == Entity.Null &&
                        m_PainterToolActive &&
                        m_OwnerLookup.TryGetComponent(entityNativeArray[i], out Owner owner2) &&
                        m_EditorContainerLookup.HasComponent(owner2.m_Owner) &&
                        m_TempLookup.TryGetComponent(owner2.m_Owner, out Temp ownerTemp) &&
                        m_OwnerLookup.TryGetComponent(ownerTemp.m_Original, out Owner originalOwner))
                    {
                        temp.m_Original = ownerTemp.m_Original;
                        temp.m_Flags |= TempFlags.Essential;
                        buffer.SetComponent(entityNativeArray[i], temp);
                        ownerTemp.m_Flags |= TempFlags.Essential;
                        ownerTemp.m_Original = originalOwner.m_Owner;
                        buffer.SetComponent(owner2.m_Owner, ownerTemp);
                        buffer.AddComponent<Hidden>(temp.m_Original);
                        buffer.AddComponent<Hidden>(ownerTemp.m_Original);
                    }

                    if (m_PainterToolActive &&
                       ((m_SelectionType == ColorPainterUISystem.SelectionType.Single &&
                        m_RaycastEntity != temp.m_Original) ||
                        m_State == ColorPainterToolSystem.State.Reseting ||
                       !m_PalettesActive ||
                       (m_SelectionType == ColorPainterUISystem.SelectionType.Radius &&
                        m_FilterType == ColorPainterUISystem.FilterType.Building &&
                        !m_BuildingLookup.HasComponent(entityNativeArray[i]))))
                    {
                        continue;
                    }

                    DynamicBuffer<AssignedPalette> paletteAssignments = buffer.AddBuffer<AssignedPalette>(instanceEntity);

                    for (int j = 0; j < System.Math.Max(m_PrefabEntities.Length, 3); j++)
                    {
                        if (m_PrefabEntities[j] == Entity.Null)
                        {
                            continue;
                        }

                        AssignedPalette newPaletteAssignment = new AssignedPalette()
                        {
                            m_Channel = j,
                            m_PaletteInstanceEntity = m_PaletteInstanceEntities[j],
                        };

                        paletteAssignments.Add(newPaletteAssignment);
                    }
                }
            }
        }


        /*
        /// <summary>
        /// Assigns a palette to the instance entity based on prefab entity and channel.
        /// </summary>
        /// <param name="instanceEntity">The entity to add the palette to.</param>
        /// <param name="prefabEntities">Palette prefab entities. Array length 3. Set entity.null if empty.</param>
        private void AssignPalettes(Entity instanceEntity, Entity[] prefabEntities, ref EntityCommandBuffer buffer)
        {
            if (!EntityManager.TryGetBuffer(instanceEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) ||
                meshColorBuffer.Length == 0)
            {
                return;
            }

            if (m_UISystem.SelectedPalettesDuringPlacement.Length < 2 ||
               (m_UISystem.SelectedPalettesDuringPlacement[0] == Entity.Null &&
                m_UISystem.SelectedPalettesDuringPlacement[1] == Entity.Null &&
                m_UISystem.SelectedPalettesDuringPlacement[2] == Entity.Null))
            {
                return;
            }

            DynamicBuffer<AssignedPalette> paletteAssignments = buffer.AddBuffer<AssignedPalette>(instanceEntity);

            RecolorSet recolorSet = new RecolorSet(meshColorBuffer[0].m_ColorSet);
            for (int i = 0; i < System.Math.Max(prefabEntities.Length, 3); i++)
            {
                if (prefabEntities[i] == Entity.Null)
                {
                    continue;
                }

                AssignedPalette newPaletteAssignment = new AssignedPalette()
                {
                    m_Channel = i,
                    m_PaletteInstanceEntity = m_PaletteInstanceManagerSystem.GetOrCreatePaletteInstanceEntity(prefabEntities[i]),
                };

                paletteAssignments.Add(newPaletteAssignment);
                m_AssignedPaletteCustomColorSystem.TryGetColorFromPalette(instanceEntity, i, out UnityEngine.Color color);
                recolorSet.Channels[i] = color;
                // m_Log.Debug($"{nameof(PalettesDuringPlacementSystem)}.{nameof(AssignPalettes)} assigned color {color} to channel {i} for entity {instanceEntity.Index} {instanceEntity.Version}.");
            }

            m_ColorPainterToolSystem.ChangeInstanceColorSet(recolorSet, ref buffer, instanceEntity);
        }
        */
    }

}
