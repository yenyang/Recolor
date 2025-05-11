// <copyright file="PalettesDuringPlacementSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Game system to handle assigning palette to Temp object entities during placement.
    /// </summary>
    public partial class PalettesDuringPlacementSystem : GameSystemBase
    {
        private ILog m_Log;
        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;
        private ObjectToolSystem m_ObjectToolSystem;
        private EntityQuery m_TempMeshColorQuery;
        private ModificationBarrier2 m_Barrier;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;
        private PaletteInstanceManagerSystem m_PaletteInstanceManagerSystem;
        private AssignedPaletteCustomColorSystem m_AssignedPaletteCustomColorSystem;
        private ColorPainterToolSystem m_ColorPainterToolSystem;
        private PalettesUISystem m_UISystem;
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
            m_UISystem = World.GetOrCreateSystemManaged<PalettesUISystem>();

            m_ToolSystem.EventToolChanged += OnToolChanged;
            m_ToolSystem.EventPrefabChanged += OnPrefabChanged;

            m_TempMeshColorQuery = SystemAPI.QueryBuilder()
                  .WithAll<Temp, PseudoRandomSeed, MeshColor>()
                  .WithNone<Deleted, Game.Objects.Plant, Owner>()
                  .Build();

            Enabled = false;
            m_Log.Info($"{nameof(PalettesDuringPlacementSystem)}.{nameof(OnCreate)}");
        }


        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            NativeArray<Entity> entities = m_TempMeshColorQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                AssignPalettes(entities[i], m_UISystem.SelectedPalettesDuringPlacement, ref buffer);
            }

            if (m_ObjectToolSystem.actualMode == ObjectToolSystem.Mode.Create &&
                entities.Length == 1 &&
                EntityManager.TryGetBuffer(entities[0], isReadOnly: true, out DynamicBuffer<MeshColor> meshColors) &&
                meshColors.Length > 0)
            {
                m_UISystem.SetNoneColors(meshColors[0].m_ColorSet);
            }
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            OnPrefabChanged(m_ToolSystem.activePrefab);
        }

        private void OnPrefabChanged(PrefabBase prefab)
        {
            if (m_ToolSystem.activeTool != m_ObjectToolSystem ||
               !Mod.Instance.Settings.ShowPalettesOptionDuringPlacement ||
               m_ObjectToolSystem.actualMode == ObjectToolSystem.Mode.Stamp ||
               m_ObjectToolSystem.actualMode == ObjectToolSystem.Mode.Move)
            {
                Enabled = false;
                m_UISystem.ShowPaletteChooserDuringPlacement = false;
                return;
            }

            if (prefab != null &&
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
                        m_UISystem.ResetNoneColors();
                        m_UISystem.UpdatePaletteChoicesDuringPlacementBinding(Mod.Instance.Settings.ResetPaletteChoicesWhenSwitchingPrefab && m_PreviousPrefabBase != prefab);
                        m_PreviousPrefabBase = prefab;
                        return;
                    }
                }
            }

            m_UISystem.ShowPaletteChooserDuringPlacement = false;
        }

        /// <summary>
        /// Assigns a palette to the instance entity based on prefab entity and channel.
        /// </summary>
        /// <param name="instanceEntity">The entity to add the palette to.</param>
        /// <param name="prefabEntities">Palette prefab entities. Array length 3. Set entity.null if empty.</param>
        private void AssignPalettes(Entity instanceEntity, Entity[] prefabEntities, ref EntityCommandBuffer buffer)
        {
            DynamicBuffer<AssignedPalette> paletteAssignments = buffer.AddBuffer<AssignedPalette>(instanceEntity);
            paletteAssignments.Clear();

            if (!EntityManager.TryGetBuffer(instanceEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) ||
                meshColorBuffer.Length == 0)
            {
                return;
            }

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
                m_Log.Debug($"{nameof(PalettesDuringPlacementSystem)}.{nameof(AssignPalettes)} assigned color {color} to channel {i} for entity {instanceEntity.Index} {instanceEntity.Version}.");
            }

            m_ColorPainterToolSystem.ChangeInstanceColorSet(recolorSet, ref buffer, instanceEntity);
        }
    }

}
