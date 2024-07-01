// <copyright file="RentersUpdatedCustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems
{
    using Colossal.Logging;
    using Game;
    using Game.Buildings;
    using Game.Common;
    using Game.Rendering;
    using Recolor.Domain;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system for overriding mesh color at right time.
    /// </summary>
    public partial class RentersUpdatedCustomMeshColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private ModificationEndBarrier m_Barrier;
        private EntityQuery m_UpdatedEventQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(CustomMeshColorSystem)}.{nameof(OnCreate)}");

            m_UpdatedEventQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Common.Event, RentersUpdated>()
                .Build();

            RequireAnyForUpdate(m_UpdatedEventQuery);
            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
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
                    buffer.AddComponent<BatchesUpdated>(rentersUpdated.m_Property);
                }
            }
        }
    }
}
