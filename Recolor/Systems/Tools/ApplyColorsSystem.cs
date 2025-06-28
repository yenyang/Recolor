// <copyright file="ApplyColorsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.Tools
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
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
                .WithAllRW<MeshColor>()
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
                m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(),
                m_MeshColorRecordLookup = SystemAPI.GetBufferLookup<MeshColorRecord>(),
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                m_AssignedPaletteLookup = SystemAPI.GetBufferLookup<AssignedPalette>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer(),
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
            public BufferLookup<CustomMeshColor> m_CustomMeshColorLookup;
            [ReadOnly]
            public BufferLookup<MeshColorRecord> m_MeshColorRecordLookup;
            [ReadOnly]
            public BufferLookup<AssignedPalette> m_AssignedPaletteLookup;
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
                        originalMeshColors.Length == 0)
                    {
                        continue;
                    }

                    ColorSet defaultColorSet = tempMeshColors[0].m_ColorSet;

                    if (!m_CustomMeshColorLookup.HasBuffer(tempEntity))
                    {
                        buffer.RemoveComponent<CustomMeshColor>(originalEntity);
                        buffer.RemoveComponent<MeshColorRecord>(originalEntity);
                    }
                    else
                    {
                        if (!m_MeshColorRecordLookup.HasBuffer(originalEntity))
                        {
                            DynamicBuffer<MeshColorRecord> meshColorRecords = buffer.AddBuffer<MeshColorRecord>(originalEntity);
                            for (int j = 0; j < originalMeshColors.Length; j++)
                            {
                                meshColorRecords.Add(new MeshColorRecord() { m_ColorSet = originalMeshColors[j].m_ColorSet });
                            }
                        }

                        DynamicBuffer<MeshColor> meshColorBuffer = buffer.AddBuffer<MeshColor>(originalEntity);
                        DynamicBuffer<CustomMeshColor> customMeshColors = buffer.AddBuffer<CustomMeshColor>(originalEntity);
                        for (int j = 0; j < originalMeshColors.Length; j++)
                        {
                            if (tempMeshColors.Length > j)
                            {
                                meshColorBuffer.Add(new MeshColor() { m_ColorSet = tempMeshColors[j].m_ColorSet });
                                customMeshColors.Add(new CustomMeshColor() { m_ColorSet = tempMeshColors[j].m_ColorSet });
                            }
                            else
                            {
                                meshColorBuffer.Add(new MeshColor() { m_ColorSet = defaultColorSet });
                                customMeshColors.Add(new CustomMeshColor { m_ColorSet = defaultColorSet });
                            }
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
