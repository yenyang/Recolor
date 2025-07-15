// <copyright file="RouteColorUpdatedCustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.SingleInstance
{
    using Colossal.Logging;
    using Game;
    using Game.Buildings;
    using Game.Common;
    using Game.Rendering;
    using Game.Routes;
    using Game.Vehicles;
    using Recolor.Domain;
    using System.Runtime.CompilerServices;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system for overriding routes vehicle colors after 
    /// </summary>
    public partial class RouteColorUpdatedCustomMeshColorSystem : GameSystemBase
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
                .WithAll<Game.Common.Event, Game.Routes.ColorUpdated>()
                .Build();

            RequireAnyForUpdate(m_UpdatedEventQuery);
            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            GatherEntitiesFromRentersUpdatedEventsJob gatherEntitiesFromRentersUpdatedEventsJob = new ()
            {
                m_RouteColorUpdatedType = SystemAPI.GetComponentTypeHandle<Game.Routes.ColorUpdated>(isReadOnly: true),
                m_RouteVehicleColorLookup = SystemAPI.GetBufferLookup<Domain.RouteVehicleColor>(isReadOnly: true),
                m_LayoutElementLookup = SystemAPI.GetBufferLookup<Game.Vehicles.LayoutElement>(isReadOnly: true),
                m_RouteVehicleLookup = SystemAPI.GetBufferLookup<Game.Routes.RouteVehicle>(isReadOnly: true),
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
            public EntityCommandBuffer buffer;
            [ReadOnly]
            public ComponentTypeHandle<Game.Routes.ColorUpdated> m_RouteColorUpdatedType;
            [ReadOnly]
            public BufferLookup<Domain.RouteVehicleColor> m_RouteVehicleColorLookup;
            [ReadOnly]
            public BufferLookup<LayoutElement> m_LayoutElementLookup;
            [ReadOnly]
            public BufferLookup<RouteVehicle> m_RouteVehicleLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Game.Routes.ColorUpdated> routeColorUpdatedNativeArray = chunk.GetNativeArray(ref m_RouteColorUpdatedType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Game.Routes.ColorUpdated routeColorUpdated = routeColorUpdatedNativeArray[i];
                    if (!m_RouteVehicleColorLookup.TryGetBuffer(routeColorUpdated.m_Route, out DynamicBuffer<RouteVehicleColor> routeVehicleColorBuffer) ||
                        !m_RouteVehicleLookup.TryGetBuffer(routeColorUpdated.m_Route, out DynamicBuffer<RouteVehicle> routeVehicleBuffer) ||
                        routeVehicleBuffer.Length == 0)
                    {
                        continue;
                    }

                    for (int j = 0; j < routeVehicleBuffer.Length; j++)
                    {
                        if (!m_LayoutElementLookup.TryGetBuffer(routeVehicleBuffer[j].m_Vehicle, out DynamicBuffer<LayoutElement> layoutElementBuffer))
                        {
                            DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(routeVehicleBuffer[j].m_Vehicle);
                            meshColorBuffer.Add(new MeshColor() { m_ColorSet = routeVehicleColorBuffer[0].m_ColorSet });
                            buffer.AddComponent<BatchesUpdated>(routeVehicleBuffer[j].m_Vehicle);
                        }
                        else
                        {
                            for (int k = 0; k < layoutElementBuffer.Length; k++)
                            {
                                DynamicBuffer<MeshColor> meshColorBuffer = buffer.SetBuffer<MeshColor>(layoutElementBuffer[k].m_Vehicle);
                                meshColorBuffer.Add(new MeshColor() { m_ColorSet = routeVehicleColorBuffer[0].m_ColorSet });
                                buffer.AddComponent<BatchesUpdated>(layoutElementBuffer[k].m_Vehicle);
                            }
                        }
                    }
                }
            }
        }
    }
}
