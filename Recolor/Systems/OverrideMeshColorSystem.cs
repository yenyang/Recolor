// <copyright file="OverrideMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Rendering;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// A system for overriding mesh color at right time.
    /// </summary>
    public partial class OverrideMeshColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_MeshColorQuery;
        private EndFrameBarrier m_Barrier;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(OverrideMeshColorSystem)}.{nameof(OnCreate)}");
            m_MeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAll<MeshColor, BatchesUpdated, CustomMeshColor>()
                   .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                   .Build();
            RequireForUpdate(m_MeshColorQuery);
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_MeshColorQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<MeshColor> meshColorBuffer) && meshColorBuffer.Length > 0)
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) && customMeshColorBuffer.Length > 0)
                {
                    continue;
                }

                MeshColor meshColor = new ()
                {
                    m_ColorSet = customMeshColorBuffer[0].m_ColorSet,
                };

                meshColorBuffer[0] = meshColor;
            }
        }


    }
}
