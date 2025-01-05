// <copyright file="ResetCustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

#define BURST
namespace Recolor.Systems.SingleInstance
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Objects;
    using Game.Rendering;
    using Recolor.Domain;
    using Recolor.Systems.SelectedInfoPanel;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
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
        private EntityQuery m_CustomMeshColorAndSubObjectsQuery;
        private EntityQuery m_CustomMeshColorAndSubLanesQuery;
        private EndFrameBarrier m_Barrier;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(ResetCustomMeshColorSystem)}.{nameof(OnCreate)}");
            m_SIPColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_CustomMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<CustomMeshColor>()
                   .WithNone<Deleted>()
                   .Build();

            m_CustomMeshColorAndSubObjectsQuery = SystemAPI.QueryBuilder()
                  .WithAll<CustomMeshColor, Game.Objects.SubObject>()
                  .WithNone<Deleted, Plant>()
                  .Build();

            m_CustomMeshColorAndSubLanesQuery = SystemAPI.QueryBuilder()
                  .WithAll<CustomMeshColor, Game.Net.SubLane>()
                  .WithNone<Deleted, Plant>()
                  .Build();

            m_MeshColorRecordQuery = SystemAPI.QueryBuilder()
                  .WithAllRW<MeshColorRecord>()
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

                CustomMeshColorSystem.BatchesUpdateForSubLanesJob batchesUpdateForSubLanesJob = new ()
                {
                    m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(isReadOnly: true),
                    m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(isReadOnly: true),
                    m_SubLaneType = SystemAPI.GetBufferTypeHandle<Game.Net.SubLane>(isReadOnly: true),
                    buffer = buffer,
                };
                Dependency = batchesUpdateForSubLanesJob.Schedule(m_CustomMeshColorAndSubLanesQuery, Dependency);
                m_Barrier.AddJobHandleForProducer(Dependency);

                CustomMeshColorSystem.BatchesUpdateForSubObjectsJob batchesUpdateForSubObjectsJob = new ()
                {
                    m_CustomMeshColorLookup = SystemAPI.GetBufferLookup<CustomMeshColor>(isReadOnly: true),
                    m_MeshColorLookup = SystemAPI.GetBufferLookup<MeshColor>(isReadOnly: true),
                    m_SubObjectLookup = SystemAPI.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true),
                    m_SubObjectType = SystemAPI.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true),
                    buffer = buffer,
                };
                Dependency = batchesUpdateForSubObjectsJob.Schedule(m_CustomMeshColorAndSubObjectsQuery, Dependency);
                m_Barrier.AddJobHandleForProducer(Dependency);

                buffer.AddComponent<BatchesUpdated>(m_CustomMeshColorQuery, EntityQueryCaptureMode.AtPlayback);
                buffer.RemoveComponent<CustomMeshColor>(m_CustomMeshColorQuery, EntityQueryCaptureMode.AtPlayback);
                buffer.RemoveComponent<MeshColorRecord>(m_MeshColorRecordQuery, EntityQueryCaptureMode.AtPlayback);
            }

            Enabled = false;
        }

    }
}
