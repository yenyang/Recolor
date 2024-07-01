// <copyright file="ResetCustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Recolor.Domain;
    using Unity.Entities;

    /// <summary>
    /// System for restting all instance color changes.
    /// </summary>
    public partial class ResetCustomMeshColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_CustomMeshColorQuery;
        private EndFrameBarrier m_Barrier;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(ResetCustomMeshColorSystem)}.{nameof(OnCreate)}");
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_CustomMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<CustomMeshColor>()
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
                buffer.RemoveComponent<CustomMeshColor>(m_CustomMeshColorQuery, EntityQueryCaptureMode.AtPlayback);
            }

            Enabled = false;
        }
    }
}
