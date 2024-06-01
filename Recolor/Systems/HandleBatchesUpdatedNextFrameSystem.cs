// <copyright file="HandleBatchesUpdatedNextFrameSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Rendering;
    using Recolor.Domain;
    using Unity.Entities;

    /// <summary>
    /// A system that adds BatchesUpdates on the next frame to entities with custom component.
    /// </summary>
    public partial class HandleBatchesUpdatedNextFrameSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_BatchesUpdatedNextFrameQuery;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandleBatchesUpdatedNextFrameSystem"/> class.
        /// </summary>
        public HandleBatchesUpdatedNextFrameSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(HandleBatchesUpdatedNextFrameSystem)} Created.");
            m_BatchesUpdatedNextFrameQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
               {
                    ComponentType.ReadOnly<BatchesUpdatedNextFrame>(),
                    ComponentType.ReadOnly<CustomMeshColor>(),
                    ComponentType.ReadOnly<MeshColor>(),
               },
                None = new ComponentType[]
               {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<BatchesUpdated>(),
               },
            });
            RequireForUpdate(m_BatchesUpdatedNextFrameQuery);
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            EntityManager.AddComponent<BatchesUpdated>(m_BatchesUpdatedNextFrameQuery);
        }
    }
}
