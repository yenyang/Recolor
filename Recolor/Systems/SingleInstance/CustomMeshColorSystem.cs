// <copyright file="CustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.SingleInstance
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Buildings;
    using Game.Common;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Routes;
    using Recolor.Domain;
    using Recolor.Extensions;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.Tools;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system for overriding mesh color at right time.
    /// </summary>
    public partial class CustomMeshColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_CustomMeshColorQuery;
        private EndFrameBarrier m_Barrier;
        private MeshColorSystem m_MeshColorSystem;
        private EntityQuery m_CustomMeshColorAndSubObjectsQuery;
        private EntityQuery m_CustomMeshColorAndSubLanesQuery;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;
        private PrefabSystem m_PrefabSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(CustomMeshColorSystem)}.{nameof(OnCreate)}");
            m_MeshColorSystem = World.GetOrCreateSystemManaged<MeshColorSystem>();
            m_SIPColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_CustomMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<MeshColor>()
                   .WithAll<BatchesUpdated, CustomMeshColor>()
                   .WithNone<Deleted, Game.Common.Overridden, Plant, Game.Creatures.Creature>()
                   .Build();

            m_CustomMeshColorAndSubObjectsQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<MeshColor>()
                   .WithAll<BatchesUpdated, CustomMeshColor, Game.Objects.SubObject>()
                   .WithNone<Deleted, Game.Common.Overridden, Plant>()
                   .Build();

            m_CustomMeshColorAndSubLanesQuery = SystemAPI.QueryBuilder()
                  .WithAllRW<MeshColor>()
                  .WithAll<BatchesUpdated, CustomMeshColor, Game.Net.SubLane>()
                  .WithNone<Deleted, Game.Common.Overridden, Plant>()
                  .Build();

            RequireAnyForUpdate(m_CustomMeshColorQuery);
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            // This overrides a query in vanilla Mesh Color System. The goal being to remove CustomMeshColor entities from MeshColorSystem.
            EntityQuery revisedUpdateQuery = GetEntityQuery(
                new EntityQueryDesc
                {
                    All = new ComponentType[1] { ComponentType.ReadOnly<MeshColor>() },
                    Any = new ComponentType[3]
                    {
                        ComponentType.ReadOnly<Updated>(),
                        ComponentType.ReadOnly<BatchesUpdated>(),
                        ComponentType.ReadOnly<Deleted>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<CustomMeshColor>(),
                    },
                }, new EntityQueryDesc
                {
                    All = new ComponentType[1] { ComponentType.ReadOnly<Game.Common.Event>() },
                    Any = new ComponentType[2]
                    {
                        ComponentType.ReadOnly<RentersUpdated>(),
                        ComponentType.ReadOnly<ColorUpdated>(),
                    },
                });

            m_MeshColorSystem.SetMemberValue("m_UpdateQuery", revisedUpdateQuery);
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game)
            {
                EntityQuery customMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<MeshColor>()
                   .WithAll<CustomMeshColor>()
                   .WithNone<Deleted, Game.Common.Overridden>()
                   .Build();

                EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
                NativeArray<Entity> entities = customMeshColorQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    m_SIPColorFieldsSystem.AddBatchesUpdatedToSubElements(entities[i], buffer);
                    buffer.AddComponent<BatchesUpdated>(entities);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_CustomMeshColorQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<MeshColor> meshColorBuffer) ||
                    meshColorBuffer.Length == 0 ||
                    !EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) ||
                    customMeshColorBuffer.Length == 0 ||
                    !EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) ||
                    !EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    continue;
                }

                for (int i = 0; i < subMeshBuffer.Length; i++)
                {
                    MeshColor meshColor = new ();
                    if (customMeshColorBuffer.Length > i)
                    {
                        meshColor.m_ColorSet = customMeshColorBuffer[i].m_ColorSet;
                    }
                    else
                    {
                        meshColor.m_ColorSet = meshColorBuffer[0].m_ColorSet;
                    }

                    meshColorBuffer[i] = meshColor;
                }
            }

            if (!m_CustomMeshColorAndSubObjectsQuery.IsEmptyIgnoreFilter)
            {
                BatchesUpdateForSubObjectsJob batchesUpdateForSubObjectsJob = new ()
                {
                    buffer = buffer,
                    m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(isReadOnly: true),
                    m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(isReadOnly: true),
                    m_SubObjectLookup = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true),
                    m_SubObjectType = SystemAPI.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true),
                };
                Dependency = batchesUpdateForSubObjectsJob.Schedule(m_CustomMeshColorAndSubObjectsQuery, Dependency);
                m_Barrier.AddJobHandleForProducer(Dependency);
            }

            if (!m_CustomMeshColorAndSubLanesQuery.IsEmptyIgnoreFilter)
            {
                BatchesUpdateForSubLanesJob batchesUpdateForSubLanesJob = new ()
                {
                    buffer = buffer,
                    m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(isReadOnly: true),
                    m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(isReadOnly: true),
                    m_SubLaneType = SystemAPI.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true),
                };
                Dependency = batchesUpdateForSubLanesJob.Schedule(m_CustomMeshColorAndSubLanesQuery, Dependency);
                m_Barrier.AddJobHandleForProducer(Dependency);
            }
        }

        /// <summary>
        /// Adds batches update for subobjects.
        /// </summary>
#if BURST
        [BurstCompile]
#endif
        public struct BatchesUpdateForSubObjectsJob : IJobChunk
        {
            /// <summary>
            /// System API buffer type handle for subobjects.
            /// </summary>
            [ReadOnly]
            public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

            /// <summary>
            /// SystemAPI buffer lookup for Mesh color.
            /// </summary>
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;

            /// <summary>
            /// SystemAPI buffer lookup for CustomMeshColor.
            /// </summary>
            [ReadOnly]
            public BufferLookup<CustomMeshColor> m_CustomMeshColorLookup;

            /// <summary>
            /// SystemAPI buffer lookup for subobjects.
            /// </summary>
            [ReadOnly]
            public BufferLookup<Game.Objects.SubObject> m_SubObjectLookup;

            /// <summary>
            /// Entity command buffer for appropriate system update phase.
            /// </summary>
            public EntityCommandBuffer buffer;

            /// <summary>
            /// Main job functionality.
            /// </summary>
            /// <param name="chunk">ECS chunks.</param>
            /// <param name="unfilteredChunkIndex">Use for parrallel jobs.</param>
            /// <param name="useEnabledMask">Not being used.</param>
            /// <param name="chunkEnabledMask">Not used either.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<Game.Objects.SubObject> subObjectBufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<Game.Objects.SubObject> subObjectBuffer = subObjectBufferAccessor[i];

                    if (subObjectBuffer.Length == 0)
                    {
                        continue;
                    }

                    foreach (Game.Objects.SubObject subObject in subObjectBuffer)
                    {
                        ProcessSubObject(subObject);

                        if (!m_SubObjectLookup.TryGetBuffer(subObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer))
                        {
                            continue;
                        }

                        foreach (Game.Objects.SubObject deepSubObject in deepSubObjectBuffer)
                        {
                            ProcessSubObject(deepSubObject);

                            if (!m_SubObjectLookup.TryGetBuffer(deepSubObject.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer2))
                            {
                                continue;
                            }

                            foreach (Game.Objects.SubObject deepSubObject2 in deepSubObjectBuffer2)
                            {
                                ProcessSubObject(deepSubObject2);

                                if (!m_SubObjectLookup.TryGetBuffer(deepSubObject2.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer3))
                                {
                                    continue;
                                }

                                foreach (Game.Objects.SubObject deepSubObject3 in deepSubObjectBuffer3)
                                {
                                    ProcessSubObject(deepSubObject3);

                                    if (!m_SubObjectLookup.TryGetBuffer(deepSubObject3.m_SubObject, out DynamicBuffer<Game.Objects.SubObject> deepSubObjectBuffer4))
                                    {
                                        continue;
                                    }

                                    foreach (Game.Objects.SubObject deepSubObject4 in deepSubObjectBuffer4)
                                    {
                                        ProcessSubObject(deepSubObject4);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private void ProcessSubObject(Game.Objects.SubObject subObject)
            {
                if (m_MeshColorLookup.HasBuffer(subObject.m_SubObject) && !m_CustomMeshColorLookup.HasBuffer(subObject.m_SubObject))
                {
                    buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                }
            }
        }

        /// <summary>
        /// Adds batches update for sublanes.
        /// </summary>
#if BURST
        [BurstCompile]
#endif
        public struct BatchesUpdateForSubLanesJob : IJobChunk
        {
            /// <summary>
            /// System API for buffer type handle sublanes.
            /// </summary>
            [ReadOnly]
            public BufferTypeHandle<Game.Net.SubLane> m_SubLaneType;

            /// <summary>
            /// SystemAPI buffer look up mesh color.
            /// </summary>
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;

            /// <summary>
            /// SystemAPI buffer lookup cusotm mesh color.
            /// </summary>
            [ReadOnly]
            public BufferLookup<CustomMeshColor> m_CustomMeshColorLookup;

            /// <summary>
            /// entity command buffer for appropriate system update phase.
            /// </summary>
            public EntityCommandBuffer buffer;

            /// <summary>
            /// Main job functionality.
            /// </summary>
            /// <param name="chunk">ECS chunks.</param>
            /// <param name="unfilteredChunkIndex">Use for parrallel jobs.</param>
            /// <param name="useEnabledMask">Not being used.</param>
            /// <param name="chunkEnabledMask">Not used either.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<Game.Net.SubLane> subLaneBufferAccessor = chunk.GetBufferAccessor(ref m_SubLaneType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<Game.Net.SubLane> subLaneBuffer = subLaneBufferAccessor[i];

                    if (subLaneBuffer.Length == 0)
                    {
                        continue;
                    }

                    foreach (Game.Net.SubLane subLane in subLaneBuffer)
                    {
                        if (m_MeshColorLookup.HasBuffer(subLane.m_SubLane) && !m_CustomMeshColorLookup.HasBuffer(subLane.m_SubLane))
                        {
                            buffer.AddComponent<BatchesUpdated>(subLane.m_SubLane);
                        }
                    }
                }
            }
        }
    }
}
