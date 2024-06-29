// <copyright file="TempCustomMeshColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// A system for handling temp entities with custom mesh colors.
    /// </summary>
    public partial class TempCustomMeshColorSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_TempMeshColorQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(TempCustomMeshColorSystem)}.{nameof(OnCreate)}");
            m_TempMeshColorQuery = SystemAPI.QueryBuilder()
                   .WithAllRW<MeshColor>()
                   .WithAll<Game.Tools.Temp>()
                   .WithNone<Deleted, Game.Common.Overridden, CustomMeshColor>()
                   .Build();

            RequireForUpdate(m_TempMeshColorQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_TempMeshColorQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out Temp temp))
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<MeshColor> meshColorBuffer) || meshColorBuffer.Length == 0)
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(temp.m_Original, isReadOnly: true, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) || customMeshColorBuffer.Length == 0)
                {
                    continue;
                }

                EntityManager.AddComponent<BatchesUpdated>(entity);
                for (int i = 0; i < meshColorBuffer.Length; i++)
                {
                    if (customMeshColorBuffer.Length > i)
                    {
                        meshColorBuffer[i] = new MeshColor() { m_ColorSet = customMeshColorBuffer[i].m_ColorSet };
                    }
                }

                DynamicBuffer<CustomMeshColor> newBuffer = EntityManager.AddBuffer<CustomMeshColor>(entity);
                foreach (CustomMeshColor customMeshColor in customMeshColorBuffer)
                {
                    newBuffer.Add(customMeshColor);
                }
            }
        }
    }
}
