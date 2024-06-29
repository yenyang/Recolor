// <copyright file="CustomColorVariationSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// A system for handling custom color variation entities that record custom color variations that can be saved per save game.
    /// </summary>
    public partial class CustomColorVariationSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_UpdatedCustomColorVariationQuery;
        private NativeHashMap<Entity, CustomColorVariation> m_CustomColorVariationMap;
        private EntityQuery m_DeletedCustomColorVariationQuery;

        /// <summary>
        /// Tries to get a custom color variation from the recorded map.
        /// </summary>
        /// <param name="prefabEntity">The prefab entity for the render prefab.</param>
        /// <param name="customColorVariation">The color set and index needed for custom color variation.</param>
        /// <returns>True if found, false if not.</returns>
        public bool TryGetCustomColorVariation(Entity prefabEntity, out CustomColorVariation customColorVariation)
        {
            customColorVariation = default;
            if (m_CustomColorVariationMap.ContainsKey(prefabEntity))
            {
                customColorVariation = m_CustomColorVariationMap[prefabEntity];
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(CustomColorVariationSystem)}.{nameof(OnCreate)}");
            m_CustomColorVariationMap = new NativeHashMap<Entity, CustomColorVariation>(0, Allocator.Persistent);
            m_UpdatedCustomColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<Updated, CustomColorVariation, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            m_DeletedCustomColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<Deleted, CustomColorVariation, PrefabRef>()
                   .Build();

            RequireAnyForUpdate(m_UpdatedCustomColorVariationQuery, m_DeletedCustomColorVariationQuery);
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            EntityQuery customColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<CustomColorVariation, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            m_CustomColorVariationMap.Clear();

            NativeArray<Entity> entities = customColorVariationQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) || !EntityManager.TryGetComponent(entity, out CustomColorVariation customColorVariation))
                {
                    continue;
                }

                if (m_CustomColorVariationMap.ContainsKey(prefabRef.m_Prefab))
                {
                    m_CustomColorVariationMap[prefabRef.m_Prefab] = customColorVariation;
                }
                else
                {
                    m_CustomColorVariationMap.Add(prefabRef.m_Prefab, customColorVariation);
                }
            }

            base.OnGameLoadingComplete(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_UpdatedCustomColorVariationQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) || !EntityManager.TryGetComponent(entity, out CustomColorVariation customColorVariation))
                {
                    continue;
                }

                if (m_CustomColorVariationMap.ContainsKey(prefabRef.m_Prefab))
                {
                    m_CustomColorVariationMap[prefabRef.m_Prefab] = customColorVariation;
                }
                else
                {
                    m_CustomColorVariationMap.Add(prefabRef.m_Prefab, customColorVariation);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_CustomColorVariationMap.Dispose();
        }
    }
}
