// <copyright file="ResetCustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.SingleInstance
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Recolor.Domain;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// System for restting all instance color changes.
    /// </summary>
    public partial class ResetCustomMeshColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_CustomMeshColorQuery;
        private EntityQuery m_MeshColorRecordQuery;
        private EntityQuery m_RouteVehicleColorQuery;
        private EntityQuery m_ServiceVehicleColorQuery;
        private EndFrameBarrier m_Barrier;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(ResetCustomMeshColorSystem)}.{nameof(OnCreate)}");
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_CustomMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<Domain.CustomMeshColor>()
                   .WithNone<Deleted>()
                   .Build();

            m_MeshColorRecordQuery = SystemAPI.QueryBuilder()
                  .WithAllRW<MeshColorRecord>()
                  .WithNone<Deleted>()
                  .Build();

            m_RouteVehicleColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<RouteVehicleColor>()
                   .WithNone<Deleted>()
                   .Build();

            m_ServiceVehicleColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<ServiceVehicleColor>()
                   .WithNone<Deleted>()
                   .Build();

            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!m_CustomMeshColorQuery.IsEmptyIgnoreFilter)
            {
                EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

                buffer.AddComponent<BatchesUpdated>(m_CustomMeshColorQuery, EntityQueryCaptureMode.AtPlayback);
                buffer.RemoveComponent<Domain.CustomMeshColor>(m_CustomMeshColorQuery, EntityQueryCaptureMode.AtPlayback);
                buffer.RemoveComponent<MeshColorRecord>(m_MeshColorRecordQuery, EntityQueryCaptureMode.AtPlayback);
                if (!m_ServiceVehicleColorQuery.IsEmptyIgnoreFilter)
                {
                    NativeArray<Entity> entities = m_ServiceVehicleColorQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (entities[i] != Entity.Null)
                        {
                            buffer.SetComponentEnabled<Game.Rendering.CustomMeshColor>(entities[i], false);
                        }
                    }
                }

                buffer.RemoveComponent<ServiceVehicleColor>(m_ServiceVehicleColorQuery, EntityQueryCaptureMode.AtPlayback);
                if (!m_RouteVehicleColorQuery.IsEmptyIgnoreFilter)
                {
                    NativeArray<Entity> entities = m_RouteVehicleColorQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (entities[i] != Entity.Null)
                        {
                            buffer.SetComponentEnabled<Game.Rendering.CustomMeshColor>(entities[i], false);
                        }
                    }
                }

                buffer.RemoveComponent<ServiceVehicleColor>(m_RouteVehicleColorQuery, EntityQueryCaptureMode.AtPlayback);
            }

            Enabled = false;
        }

    }
}
