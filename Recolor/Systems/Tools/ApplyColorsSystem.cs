// <copyright file="ApplyColorsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.Tools
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Game.Vehicles;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Systems.SelectedInfoPanel;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// A system to apply colors or palettes from color painter tool.
    /// </summary>
    public partial class ApplyColorsSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_TempCustomMeshColorQuery;
        private ToolSystem m_ToolSystem;
        private ColorPainterToolSystem m_ColorPainterToolSystem;
        private ColorPainterUISystem m_UISystem;
        private ToolOutputBarrier m_Barrier;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;

        /// <inheritdoc/>
        protected override void OnCreate ()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_ColorPainterToolSystem = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_SIPColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();

            m_ToolSystem.EventToolChanged += OnToolChanged;

            m_TempCustomMeshColorQuery = SystemAPI.QueryBuilder()
                .WithAllRW<MeshColor, Game.Rendering.CustomMeshColor>()
                .WithAll<Temp>()
                .WithNone<Deleted, Game.Common.Overridden>()
                .Build();

            RequireForUpdate(m_TempCustomMeshColorQuery);
            m_Log.Info($"{nameof(ApplyColorsSystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_Log.Debug($"{nameof(ApplyColorsSystem)}.{nameof(OnUpdate)} ");
            if (!m_SIPColorFieldsSystem.SingleInstance)
            {
                return;
            }

            ChangeMeshColorJob changeMeshColorJob = new ChangeMeshColorJob()
            {
                m_RecolorCustomMeshColorLookup = SystemAPI.GetBufferLookup<Domain.CustomMeshColor>(isReadOnly: true),
                m_VanillaCustomMeshColorLookup = SystemAPI.GetBufferLookup<Game.Rendering.CustomMeshColor>(isReadOnly: true),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(isReadOnly: true),
                m_MeshColorRecordLookup = SystemAPI.GetBufferLookup<MeshColorRecord>(isReadOnly: true),
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                m_AssignedPaletteLookup = SystemAPI.GetBufferLookup<AssignedPalette>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer(),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<Game.Prefabs.PrefabRef>(isReadOnly: true),
                m_PrefabSubMeshLookup = SystemAPI.GetBufferLookup<Game.Prefabs.SubMesh>(isReadOnly: true),
            };

            JobHandle jobHandle = changeMeshColorJob.Schedule(m_TempCustomMeshColorQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool == m_ColorPainterToolSystem)
            {
                Enabled = true;
                return;
            }

            Enabled = false;
        }

#if BURST
        [BurstCompile]
#endif
        private struct ChangeMeshColorJob : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Temp> m_TempType;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;
            [ReadOnly]
            public BufferLookup<Domain.CustomMeshColor> m_RecolorCustomMeshColorLookup;
            [ReadOnly]
            public BufferLookup<MeshColorRecord> m_MeshColorRecordLookup;
            [ReadOnly]
            public BufferLookup<AssignedPalette> m_AssignedPaletteLookup;
            [ReadOnly]
            public BufferLookup<Game.Rendering.CustomMeshColor> m_VanillaCustomMeshColorLookup;
            [ReadOnly]
            public BufferLookup<Game.Prefabs.SubMesh> m_PrefabSubMeshLookup;
            [ReadOnly]
            public ComponentLookup<Game.Prefabs.PrefabRef> m_PrefabRefLookup;

            public EntityCommandBuffer buffer;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity tempEntity = entityNativeArray[i];
                    Entity originalEntity = tempNativeArray[i].m_Original;

                    if (!m_MeshColorLookup.TryGetBuffer(originalEntity, out DynamicBuffer<MeshColor> originalMeshColors) ||
                        !m_MeshColorLookup.TryGetBuffer(tempEntity, out DynamicBuffer<MeshColor> tempMeshColors) ||
                        tempMeshColors.Length == 0 ||
                        originalMeshColors.Length == 0 ||
                        !m_PrefabRefLookup.TryGetComponent(originalEntity, out PrefabRef prefabRef) ||
                        !m_PrefabSubMeshLookup.TryGetBuffer(prefabRef.m_Prefab, out DynamicBuffer<SubMesh> submeshes) ||
                        submeshes.Length <= 0)
                    {
                        continue;
                    }

                    ColorSet defaultColorSet = tempMeshColors[0].m_ColorSet;

                    if (!m_RecolorCustomMeshColorLookup.HasBuffer(tempEntity))
                    {
                        DynamicBuffer<Game.Rendering.CustomMeshColor> customMeshColors = buffer.SetBuffer<Game.Rendering.CustomMeshColor>(originalEntity);
                        customMeshColors.Clear();
                        buffer.SetComponentEnabled<Game.Rendering.CustomMeshColor>(originalEntity, false);
                        buffer.RemoveComponent<MeshColorRecord>(originalEntity);
                    }
                    else
                    {
                        bool completeMatch = true;
                        if (!m_MeshColorRecordLookup.TryGetBuffer(originalEntity, out DynamicBuffer<MeshColorRecord> meshColorRecord))
                        {
                            DynamicBuffer<MeshColorRecord> meshColorRecords = buffer.AddBuffer<MeshColorRecord>(originalEntity);
                            for (int j = 0; j < originalMeshColors.Length; j++)
                            {
                                meshColorRecords.Add(new MeshColorRecord() { m_ColorSet = originalMeshColors[j].m_ColorSet });
                            }

                            completeMatch = false;
                        }
                        else
                        {
                            if (meshColorRecord.Length > 0 &&
                                tempMeshColors.Length > 0)
                            {
                                ColorSet newColorSet = tempMeshColors[0].m_ColorSet;
                                for (int k = 0; k < 3; k++)
                                {
                                    if (newColorSet[k] != meshColorRecord[0].m_ColorSet[k])
                                    {
                                        completeMatch = false;
                                    }
                                }
                            }
                        }

                        if (!completeMatch)
                        {
                            DynamicBuffer<MeshColor> meshColorBuffer = buffer.AddBuffer<MeshColor>(originalEntity);
                            DynamicBuffer<Game.Rendering.CustomMeshColor> customMeshColors = buffer.AddBuffer<Game.Rendering.CustomMeshColor>(originalEntity);
                            for (int j = 0; j < submeshes.Length; j++)
                            {
                                if (tempMeshColors.Length > j)
                                {
                                    meshColorBuffer.Add(new MeshColor() { m_ColorSet = tempMeshColors[j].m_ColorSet });
                                    customMeshColors.Add(new Game.Rendering.CustomMeshColor() { m_ColorSet = tempMeshColors[j].m_ColorSet });
                                }
                                else
                                {
                                    meshColorBuffer.Add(new MeshColor() { m_ColorSet = defaultColorSet });
                                    customMeshColors.Add(new Game.Rendering.CustomMeshColor { m_ColorSet = defaultColorSet });
                                }
                            }

                            // Added for better Multiple Submesh Support.
                            if (submeshes.Length > 1 &&
                                m_RecolorCustomMeshColorLookup.TryGetBuffer(tempEntity, out DynamicBuffer<Domain.CustomMeshColor> tempRecolorCustomMeshColors))
                            {
                                DynamicBuffer<Domain.CustomMeshColor> recolorCustomMeshColors = buffer.AddBuffer<Domain.CustomMeshColor>(originalEntity);
                                recolorCustomMeshColors.CopyFrom(tempRecolorCustomMeshColors);
                            }

                            buffer.SetComponentEnabled<Game.Rendering.CustomMeshColor>(originalEntity, true);
                        }
                        else
                        {
                            DynamicBuffer<Game.Rendering.CustomMeshColor> customMeshColors = buffer.SetBuffer<Game.Rendering.CustomMeshColor>(originalEntity);
                            customMeshColors.Clear();
                            buffer.SetComponentEnabled<Game.Rendering.CustomMeshColor>(originalEntity, false);
                            buffer.RemoveComponent<MeshColorRecord>(originalEntity);
                        }
                    }

                    if ((!m_AssignedPaletteLookup.HasBuffer(tempEntity) &&
                         m_AssignedPaletteLookup.HasBuffer(originalEntity)) ||
                        (m_AssignedPaletteLookup.TryGetBuffer(tempEntity, out DynamicBuffer<AssignedPalette> newPalleteAssignment) &&
                         newPalleteAssignment.Length == 0))
                    {
                        buffer.RemoveComponent<AssignedPalette>(originalEntity);
                    }
                    else if (m_AssignedPaletteLookup.TryGetBuffer(tempEntity, out DynamicBuffer<AssignedPalette> newPalleteAssignment1) &&
                             newPalleteAssignment.Length > 0)
                    {
                        DynamicBuffer<AssignedPalette> assignedPalettes = buffer.AddBuffer<AssignedPalette>(originalEntity);
                        assignedPalettes.CopyFrom(newPalleteAssignment1);
                    }
                }
            }
        }

    }
}
