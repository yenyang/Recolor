// <copyright file="TempCustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.SingleInstance
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system for handling temp entities with custom mesh colors.
    /// </summary>
    public partial class TempCustomMeshColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_TempMeshColorQuery;
        private ModificationEndBarrier m_Barrier;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(TempCustomMeshColorSystem)}.{nameof(OnCreate)}");
            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            m_TempMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAll<Game.Tools.Temp, MeshColor>()
                   .WithNone<Deleted, Game.Common.Overridden, CustomMeshColor>()
                   .Build();

            RequireForUpdate(m_TempMeshColorQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            TempCustomMeshColorJob tempCustomMeshColorJob = new TempCustomMeshColorJob()
            {
                m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(isReadOnly: true),
                m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(isReadOnly: true),
                buffer = m_Barrier.CreateCommandBuffer(),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_TempType = SystemAPI.GetComponentTypeHandle<Temp>(),
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

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Temp> tempNativeArray = chunk.GetNativeArray(ref m_TempType);
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Temp temp = tempNativeArray[i];
                    if (!m_CustomMeshColorLookup.TryGetBuffer(temp.m_Original, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer))
                    {
                        continue;
                    }

                    DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(entityNativeArray[i]);

                    meshColorBuffer.Add(new MeshColor() { m_ColorSet = customMeshColorBuffer[0].m_ColorSet });
                    buffer.AddBuffer<CustomMeshColor>(entityNativeArray[i]);
                    foreach (CustomMeshColor customMeshColor in customMeshColorBuffer)
                    {
                        buffer.AppendToBuffer(entityNativeArray[i], customMeshColor);
                    }

                    buffer.AddComponent<BatchesUpdated>(entityNativeArray[i]);
                }
            }
        }
    }
}
