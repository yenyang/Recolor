// <copyright file="TempCustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.SingleInstance
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Systems.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// A system for handling temp entities with custom mesh colors.
    /// </summary>
    public partial class TempCustomMeshColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_TempMeshColorQuery;
        private ModificationEndBarrier m_Barrier;
        private ColorPainterToolSystem m_ColorPainterTool;
        private ToolSystem m_ToolSystem;
        private ColorPainterUISystem m_UISystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(TempCustomMeshColorSystem)}.{nameof(OnCreate)}");
            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            m_ColorPainterTool = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_TempMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAll<Game.Tools.Temp, MeshColor>()
                   .WithNone<Deleted, Game.Common.Overridden, CustomMeshColor>()
                   .Build();

            RequireForUpdate(m_TempMeshColorQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            TempCustomMeshColorJob tempCustomMeshColorJob = new()
            {
                m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(isReadOnly: true),
                m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
                m_PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_SubMeshLookup = SystemAPI.GetBufferLookup<SubMesh>(isReadOnly: true),
                m_PainterToolActive = m_ToolSystem.activeTool == m_ColorPainterTool,
                m_PainterToolMode = m_UISystem.ToolMode,
                m_ColorSet = m_UISystem.RecolorSet.GetColorSet(),
                m_Toggles = m_UISystem.RecolorSet.GetChannelToggles(),
            };
            JobHandle jobHandle = tempCustomMeshColorJob.Schedule(m_TempMeshColorQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

#if BURST
        [BurstCompile]
#endif
        private struct TempCustomMeshColorJob : IJobChunk
        {
            [ReadOnly]
            public BufferLookup<CustomMeshColor> m_CustomMeshColorLookup;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;
            public EntityTypeHandle m_EntityType;
            public EntityCommandBuffer buffer;
            [ReadOnly]
            public ComponentTypeHandle<Temp> m_TempType;
            [ReadOnly]
            public BufferLookup<SubMesh> m_SubMeshLookup;
            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefLookup;
            public bool m_PainterToolActive;
            public ColorSet m_ColorSet;
            public bool3 m_Toggles;
            public ColorPainterUISystem.PainterToolMode m_PainterToolMode;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Temp temp = tempNativeArray[i];
                    if (!m_PrefabRefLookup.TryGetComponent(temp.m_Original, out PrefabRef prefabRef) ||
                        !m_SubMeshLookup.TryGetBuffer(prefabRef.m_Prefab, out DynamicBuffer<SubMesh> subMeshBuffer))
                    {
                        continue;
                    }


                    if (m_CustomMeshColorLookup.TryGetBuffer(temp.m_Original, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) &&
                       (!m_PainterToolActive ||
                        m_PainterToolMode == ColorPainterUISystem.PainterToolMode.Picker))
                    {
                        DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(entityNativeArray[i]);
                        for (int j = 0; j < subMeshBuffer.Length; j++)
                        {
                            if (customMeshColorBuffer.Length > j)
                            {
                                meshColorBuffer.Add(new () { m_ColorSet = customMeshColorBuffer[j].m_ColorSet });
                            }
                            else
                            {
                                meshColorBuffer.Add(new () { m_ColorSet = customMeshColorBuffer[0].m_ColorSet });
                            }
                        }

                        DynamicBuffer<CustomMeshColor> newCustomMeshColorBuffer = buffer.AddBuffer<CustomMeshColor>(entityNativeArray[i]);

                        for (int j = 0; j < subMeshBuffer.Length; j++)
                        {
                            if (customMeshColorBuffer.Length > j)
                            {
                                newCustomMeshColorBuffer.Add(customMeshColorBuffer[j]);
                            }
                            else
                            {
                                newCustomMeshColorBuffer.Add(customMeshColorBuffer[0]);
                            }
                        }
                    }
                    else if (m_PainterToolActive &&
                             m_PainterToolMode == ColorPainterUISystem.PainterToolMode.Paint &&
                             m_MeshColorLookup.TryGetBuffer(temp.m_Original, out DynamicBuffer<MeshColor> originalMeshColor))
                    {
                        DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(entityNativeArray[i]);
                        DynamicBuffer<CustomMeshColor> newCustomMeshColorBuffer = buffer.AddBuffer<CustomMeshColor>(entityNativeArray[i]);
                        for (int j = 0; j < subMeshBuffer.Length; j++)
                        {
                            ColorSet newColorSet = m_ColorSet;
                            if (originalMeshColor.Length > j)
                            {
                                for (int k = 0; k < 3; k++)
                                {
                                    if (!m_Toggles[k] &&
                                        originalMeshColor.Length > j)
                                    {
                                        newColorSet[k] = originalMeshColor[j].m_ColorSet[k];
                                    }
                                }
                            }

                            meshColorBuffer.Add(new () { m_ColorSet = newColorSet });
                            newCustomMeshColorBuffer.Add(new () { m_ColorSet = newColorSet });
                        }
                    }
                    else
                    {
                        continue;
                    }

                    buffer.AddComponent<BatchesUpdated>(entityNativeArray[i]);
                }
            }
        }
    }
}
