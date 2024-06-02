// <copyright file="OverrideMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
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
    using Game.Simulation;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Extensions;
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
        private EntityQuery m_UpdatedEventQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(CustomMeshColorSystem)}.{nameof(OnCreate)}");
            m_MeshColorSystem = World.GetOrCreateSystemManaged<MeshColorSystem>();
            m_CustomMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<MeshColor>()
                   .WithAll<BatchesUpdated, CustomMeshColor>()
                   .WithNone<Deleted, Game.Common.Overridden, Plant>()
                   .Build();

            m_UpdatedEventQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Common.Event, RentersUpdated>()
                .Build();

            RequireAnyForUpdate(m_CustomMeshColorQuery, m_UpdatedEventQuery);
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
                foreach (Entity entity in entities)
                {
                    buffer.AddComponent<BatchesUpdated>(entity);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_CustomMeshColorQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<MeshColor> meshColorBuffer) || meshColorBuffer.Length == 0)
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) || customMeshColorBuffer.Length == 0)
                {
                    continue;
                }

                for (int i = 0; i < meshColorBuffer.Length; i++)
                {
                    MeshColor meshColor = new ()
                    {
                        m_ColorSet = customMeshColorBuffer[i].m_ColorSet,
                    };

                    meshColorBuffer[i] = meshColor;
                }
            }

            GatherEntitiesFromRentersUpdatedEventsJob gatherEntitiesFromRentersUpdatedEventsJob = new GatherEntitiesFromRentersUpdatedEventsJob()
            {
                m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(isReadOnly: true),
                m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(isReadOnly: true),
                m_RentersUpdatedType = SystemAPI.GetComponentTypeHandle<RentersUpdated>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer(),
            };
            JobHandle jobHandle = gatherEntitiesFromRentersUpdatedEventsJob.Schedule(m_UpdatedEventQuery, Dependency);
            m_Barrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

#if BURST
        [BurstCompile]
#endif
        private struct GatherEntitiesFromRentersUpdatedEventsJob : IJobChunk
        {
            [ReadOnly]
            public BufferLookup<CustomMeshColor> m_CustomMeshColorLookup;
            [ReadOnly]
            public BufferLookup<MeshColor> m_MeshColorLookup;
            public EntityCommandBuffer buffer;
            [ReadOnly]
            public ComponentTypeHandle<RentersUpdated> m_RentersUpdatedType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<RentersUpdated> rentersUpdatedNativeArray = chunk.GetNativeArray(ref m_RentersUpdatedType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    RentersUpdated rentersUpdated = rentersUpdatedNativeArray[i];
                    if (!m_CustomMeshColorLookup.TryGetBuffer(rentersUpdated.m_Property, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer))
                    {
                        continue;
                    }

                    DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(rentersUpdated.m_Property);

                    meshColorBuffer.Add(new MeshColor() { m_ColorSet = customMeshColorBuffer[0].m_ColorSet });
                    buffer.AddComponent<BatchesUpdatedNextFrame>(rentersUpdated.m_Property);
                }
            }
        }
    }
}
