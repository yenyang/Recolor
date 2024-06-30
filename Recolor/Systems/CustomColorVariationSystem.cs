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
    using Recolor.Domain;
    using Unity.Collections;
    using Unity.Entities;
    using static Recolor.Systems.SelectedInfoPanelColorFieldsSystem;

    /// <summary>
    /// A system for handling custom color variation entities that record custom color variations that can be saved per save game.
    /// </summary>
    public partial class CustomColorVariationSystem : GameSystemBase
    {
        private ILog m_Log;
        private EntityQuery m_UpdatedCustomColorVariationQuery;
        private NativeHashMap<Entity, Entity> m_CustomColorVariationMap;
        private EntityQuery m_DeletedCustomColorVariationQuery;
        private EntityArchetype m_CustomColorVariationArchetype;
        private SelectedInfoPanelColorFieldsSystem m_SelectedInfoPanelColorFieldsSystem;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Tries to get a custom color variation from the recorded map.
        /// </summary>
        /// <param name="prefabEntity">The prefab entity for the render prefab.</param>
        /// <param name="customColorVariation">The color set and index needed for custom color variation.</param>
        /// <returns>True if found, false if not.</returns>
        public bool TryGetCustomColorVariation(Entity prefabEntity, out CustomColorVariation customColorVariation)
        {
            customColorVariation = default;
            if (m_CustomColorVariationMap.ContainsKey(prefabEntity) && m_CustomColorVariationMap[prefabEntity] != Entity.Null && EntityManager.TryGetComponent(m_CustomColorVariationMap[prefabEntity], out customColorVariation))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Schedules creation of a CustomColorVariationEntity.
        /// </summary>
        /// <param name="buffer">ECB for scheduling.</param>
        /// <param name="prefabEntity">Prefab entity for RenderPrefab.</param>
        /// <param name="colorSet">Custom color set.</param>
        /// <param name="index">Index to make change to.</param>
        public void CreateOrUpdateCustomColorVariationEntity(EntityCommandBuffer buffer, Entity prefabEntity, ColorSet colorSet, int index)
        {
            if (!m_CustomColorVariationMap.ContainsKey(prefabEntity))
            {
                Entity customColorVariationEntity = buffer.CreateEntity(m_CustomColorVariationArchetype);
                buffer.SetComponent(customColorVariationEntity, new PrefabRef(prefabEntity));
                buffer.SetComponent(customColorVariationEntity, new CustomColorVariation(colorSet, index));
                buffer.AddComponent<Updated>(customColorVariationEntity);
                m_CustomColorVariationMap.Add(prefabEntity, Entity.Null);
            }
            else if (EntityManager.HasComponent<CustomColorVariation>(m_CustomColorVariationMap[prefabEntity]) && m_CustomColorVariationMap[prefabEntity] != Entity.Null)
            {
                buffer.SetComponent(m_CustomColorVariationMap[prefabEntity], new CustomColorVariation(colorSet, index));
                buffer.AddComponent<Updated>(m_CustomColorVariationMap[prefabEntity]);
            }
        }

        /// <summary>
        /// Schedules creation of a CustomColorVariationEntity.
        /// </summary>
        /// <param name="buffer">ECB for scheduling.</param>
        /// <param name="prefabEntity">Prefab entity for RenderPrefab.</param>
        public void DeleteCustomColorVariationEntity(EntityCommandBuffer buffer, Entity prefabEntity)
        {
            if (m_CustomColorVariationMap.ContainsKey(prefabEntity))
            {
                buffer.AddComponent<Deleted>(m_CustomColorVariationMap[prefabEntity]);
                m_CustomColorVariationMap.Remove(prefabEntity);
            }
        }


        /// <summary>
        /// Reloads custom color variations that from custom color variation entities.
        /// </summary>
        /// <param name="buffer">ECB for recording commands.</param>
        public void ReloadCustomColorVariations(EntityCommandBuffer buffer)
        {
            EntityQuery customColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<CustomColorVariation, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            NativeArray<Entity> customColorVariationEntities = customColorVariationQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> prefabsNeedingUpdates = new NativeList<Entity>(Allocator.Temp);
            foreach (Entity entity in customColorVariationEntities)
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                {
                    continue;
                }

                if (!TryGetCustomColorVariation(prefabRef.m_Prefab, out CustomColorVariation customColorVariation))
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer))
                {
                    continue;
                }

                if (!m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabBase))
                {
                    continue;
                }

                if (colorVariationBuffer.Length > customColorVariation.m_Index)
                {
                    ColorVariation currentColorVariation = colorVariationBuffer[customColorVariation.m_Index];

                    currentColorVariation.m_ColorSet = customColorVariation.m_ColorSet;
                    colorVariationBuffer[customColorVariation.m_Index] = currentColorVariation;
                    if (!prefabsNeedingUpdates.Contains(prefabRef.m_Prefab))
                    {
                        prefabsNeedingUpdates.Add(prefabRef.m_Prefab);
                    }

                    m_SelectedInfoPanelColorFieldsSystem.TryGetSeasonFromColorGroupID(currentColorVariation.m_GroupID, out Season season);
                    m_Log.Info($"{nameof(CustomColorVariationSystem)}.{nameof(ReloadCustomColorVariations)} Loaded Colorset for {prefabBase.GetPrefabID()} in {season}");
                }
            }

            if (prefabsNeedingUpdates.Length == 0)
            {
                return;
            }

            EntityQuery prefabRefQuery = SystemAPI.QueryBuilder()
                .WithAll<PrefabRef>()
                .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                .Build();

            NativeArray<Entity> entities = prefabRefQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in entities)
            {
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer) && prefabsNeedingUpdates.Contains(currentSubMeshBuffer[0].m_SubMesh))
                {
                    buffer.AddComponent<BatchesUpdated>(e);
                }
            }
        }


        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(CustomColorVariationSystem)}.{nameof(OnCreate)}");
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_SelectedInfoPanelColorFieldsSystem = World.GetOrCreateSystemManaged<SelectedInfoPanelColorFieldsSystem>();
            m_CustomColorVariationMap = new NativeHashMap<Entity, Entity>(0, Allocator.Persistent);

            m_CustomColorVariationArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<CustomColorVariation>(), ComponentType.ReadWrite<PrefabRef>());

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
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);

            EntityQuery customColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<CustomColorVariation, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            NativeArray<Entity> entities = customColorVariationQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    continue;
                }

                if (!TryGetCustomColorVariation(prefabRef.m_Prefab, out CustomColorVariation customColorVariation))
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    continue;
                }

                if (!EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer))
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    continue;
                }

                if (!m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabBase))
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    continue;
                }

                if (colorVariationBuffer.Length > customColorVariation.m_Index)
                {
                    ColorVariation currentColorVariation = colorVariationBuffer[customColorVariation.m_Index];

                    m_SelectedInfoPanelColorFieldsSystem.TryGetSeasonFromColorGroupID(currentColorVariation.m_GroupID, out Season season);
                    AssetSeasonIdentifier assetSeasonIdentifier = new AssetSeasonIdentifier()
                    {
                        m_Index = customColorVariation.m_Index,
                        m_PrefabID = prefabBase.GetPrefabID(),
                        m_Season = season,
                    };

                    if (!m_SelectedInfoPanelColorFieldsSystem.TryGetVanillaColorSet(assetSeasonIdentifier, out currentColorVariation.m_ColorSet))
                    {
                        EntityManager.AddComponent<Deleted>(entity);
                        continue;
                    }

                    colorVariationBuffer[customColorVariation.m_Index] = currentColorVariation;
                    m_Log.Info($"{nameof(CustomColorVariationSystem)}.{nameof(OnGamePreload)} Reset Colorset for {prefabBase.GetPrefabID()} in {season}");
                }

                EntityManager.AddComponent<Deleted>(entity);
            }
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
                if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) || !EntityManager.HasComponent<CustomColorVariation>(entity))
                {
                    continue;
                }

                if (m_CustomColorVariationMap.ContainsKey(prefabRef.m_Prefab))
                {
                    m_CustomColorVariationMap[prefabRef.m_Prefab] = entity;
                }
                else
                {
                    m_CustomColorVariationMap.Add(prefabRef.m_Prefab, entity);
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
                    m_CustomColorVariationMap[prefabRef.m_Prefab] = entity;
                }
                else
                {
                    m_CustomColorVariationMap.Add(prefabRef.m_Prefab, entity);
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
