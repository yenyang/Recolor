// <copyright file="CustomColorVariationSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.ColorVariations
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Recolor.Domain;
    using Recolor.Systems.SelectedInfoPanel;
    using Unity.Collections;
    using Unity.Entities;
    using static Recolor.Systems.SelectedInfoPanel.SIPColorFieldsSystem;

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
        private SIPColorFieldsSystem m_SelectedInfoPanelColorFieldsSystem;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Tries to get a custom color variation from the recorded map.
        /// </summary>
        /// <param name="prefabEntity">The prefab entity for the render prefab.</param>
        /// <param name="index">the color variation index.</param>
        /// <param name="customColorVariation">The color set and index needed for custom color variation.</param>
        /// <returns>True if found, false if not.</returns>
        public bool TryGetCustomColorVariation(Entity prefabEntity, int index, out CustomColorVariations customColorVariation)
        {
            customColorVariation = default;
            if (m_CustomColorVariationMap.ContainsKey(prefabEntity) && m_CustomColorVariationMap[prefabEntity] != Entity.Null && EntityManager.TryGetBuffer(m_CustomColorVariationMap[prefabEntity], isReadOnly: true, out DynamicBuffer<CustomColorVariations> customColorVariations))
            {
                foreach (CustomColorVariations currentVariation in customColorVariations)
                {
                    if (currentVariation.m_Index == index)
                    {
                        customColorVariation = currentVariation;
                        return true;
                    }
                }
            }

            return false;
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
                buffer.AddBuffer<CustomColorVariations>(customColorVariationEntity);
                buffer.AppendToBuffer(customColorVariationEntity, new CustomColorVariations(colorSet, index));
                buffer.AddComponent<Updated>(customColorVariationEntity);
                m_CustomColorVariationMap.Add(prefabEntity, Entity.Null);
            }
            else if (EntityManager.HasBuffer<CustomColorVariations>(m_CustomColorVariationMap[prefabEntity]) && m_CustomColorVariationMap[prefabEntity] != Entity.Null && !TryGetCustomColorVariation(prefabEntity, index, out CustomColorVariations customColorVariation))
            {
                buffer.AppendToBuffer(m_CustomColorVariationMap[prefabEntity], new CustomColorVariations(colorSet, index));
                buffer.AddComponent<Updated>(m_CustomColorVariationMap[prefabEntity]);
            }
            else if (EntityManager.TryGetBuffer(m_CustomColorVariationMap[prefabEntity], isReadOnly: false, out DynamicBuffer<CustomColorVariations> customColorVariations) && m_CustomColorVariationMap[prefabEntity] != Entity.Null && TryGetCustomColorVariation(prefabEntity, index, out CustomColorVariations matchingIndexVariation)) 
            {
                for (int i = 0; i < customColorVariations.Length; i++)
                {
                    if (customColorVariations[i].m_Index == index)
                    {
                        CustomColorVariations currentColorVariation = customColorVariations[i];
                        currentColorVariation.m_ColorSet = colorSet;
                        customColorVariations[i] = currentColorVariation;
                        return;
                    }
                }
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
                   .WithAll<CustomColorVariations, PrefabRef>()
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

                if (!TryGetCustomColorVariationsBuffer(prefabRef.m_Prefab, true, out DynamicBuffer<CustomColorVariations> customColorVariations))
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

                for (int i = 0; i < customColorVariations.Length; i++)
                {
                    CustomColorVariations customColorVariation = customColorVariations[i];
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
                        m_Log.Debug($"{nameof(CustomColorVariationSystem)}.{nameof(ReloadCustomColorVariations)} Loaded Colorset for {prefabBase.GetPrefabID()} in {season}");
                    }
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

        /// <summary>
        /// Resets all custom color variations and deletes all the entities.
        /// </summary>
        public void ResetAllCustomColorVariations()
        {
            EntityQuery customColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<CustomColorVariations, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            NativeArray<Entity> customColorVariationEntities = customColorVariationQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> prefabsNeedingUpdates = new NativeList<Entity>(Allocator.Temp);
            foreach (Entity entity in customColorVariationEntities)
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                {
                    EntityManager.AddComponent<Deleted>(entity);
                    continue;
                }

                if (!TryGetCustomColorVariationsBuffer(prefabRef.m_Prefab, true, out DynamicBuffer<CustomColorVariations> customColorVariations))
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

                for (int i = 0; i < customColorVariations.Length; i++)
                {
                    CustomColorVariations customColorVariation = customColorVariations[i];
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

                        if (!prefabsNeedingUpdates.Contains(prefabRef.m_Prefab))
                        {
                            prefabsNeedingUpdates.Add(prefabRef.m_Prefab);
                        }

                        if (!m_SelectedInfoPanelColorFieldsSystem.TryGetVanillaColorSet(assetSeasonIdentifier, out currentColorVariation.m_ColorSet))
                        {
                            EntityManager.AddComponent<Deleted>(entity);
                            continue;
                        }

                        colorVariationBuffer[customColorVariation.m_Index] = currentColorVariation;
                        m_Log.Debug($"{nameof(CustomColorVariationSystem)}.{nameof(ResetAllCustomColorVariations)} Reset Colorset for {prefabBase.GetPrefabID()} in {season}");
                    }
                }

                EntityManager.AddComponent<Deleted>(entity);
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
                    EntityManager.AddComponent<BatchesUpdated>(e);
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
            m_SelectedInfoPanelColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();
            m_CustomColorVariationMap = new NativeHashMap<Entity, Entity>(0, Allocator.Persistent);

            m_CustomColorVariationArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<CustomColorVariations>(), ComponentType.ReadWrite<PrefabRef>());

            m_UpdatedCustomColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<Updated, CustomColorVariations, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            m_DeletedCustomColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<Deleted, CustomColorVariations, PrefabRef>()
                   .Build();

            RequireAnyForUpdate(m_UpdatedCustomColorVariationQuery, m_DeletedCustomColorVariationQuery);
        }

        /// <inheritdoc/>
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);

            ResetAllCustomColorVariations();
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            // Handles migration from component to buffer.
            EntityQuery retiredCustomColorVariationQuery = SystemAPI.QueryBuilder()
                   .WithAll<CustomColorVariation, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            if (!retiredCustomColorVariationQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> retiredEntities = retiredCustomColorVariationQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in retiredEntities)
                {
                    if (EntityManager.TryGetComponent(entity, out CustomColorVariation customColorVariation))
                    {
                        DynamicBuffer<CustomColorVariations> customColorVariations = EntityManager.AddBuffer<CustomColorVariations>(entity);
                        customColorVariations.Add(new CustomColorVariations(customColorVariation.m_ColorSet, customColorVariation.m_Index));
                        EntityManager.RemoveComponent<CustomColorVariation>(entity);
                    }
                    else
                    {
                        EntityManager.AddComponent<Deleted>(entity);
                    }
                }
            }

            EntityQuery customColorVariationsQuery = SystemAPI.QueryBuilder()
                   .WithAll<CustomColorVariations, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            m_CustomColorVariationMap.Clear();

            NativeArray<Entity> entities = customColorVariationsQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) || !EntityManager.HasBuffer<CustomColorVariations>(entity))
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
            if (!m_UpdatedCustomColorVariationQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_UpdatedCustomColorVariationQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in entities)
                {
                    if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) || !EntityManager.HasBuffer<CustomColorVariations>(entity))
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

            if (!m_DeletedCustomColorVariationQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_DeletedCustomColorVariationQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in entities)
                {
                    if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
                    {
                        continue;
                    }

                    if (m_CustomColorVariationMap.ContainsKey(prefabRef.m_Prefab))
                    {
                        m_CustomColorVariationMap.Remove(prefabRef.m_Prefab);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_CustomColorVariationMap.Dispose();
        }

        private bool TryGetCustomColorVariationsBuffer(Entity prefabEntity, bool isReadOnly, out DynamicBuffer<CustomColorVariations> customColorVariations)
        {
            customColorVariations = default;
            if (m_CustomColorVariationMap.ContainsKey(prefabEntity) && m_CustomColorVariationMap[prefabEntity] != Entity.Null)
            {
                return EntityManager.TryGetBuffer(m_CustomColorVariationMap[prefabEntity], isReadOnly: isReadOnly, out customColorVariations);
            }

            return false;
        }
    }
}
