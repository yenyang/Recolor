// <copyright file="PaletteInstanceManagerSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System;
    using System.Collections.Generic;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Rendering;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Domain.Palette.Prefabs;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// System that handles palette instance entities.
    /// </summary>
    public partial class PaletteInstanceManagerSystem : GameSystemBase
    {
        private ILog m_Log;
        private NativeHashMap<Entity, Entity> m_PaletteInstanceMap;
        private EntityQuery m_UpdatedPaletteInstanceQuery;
        private EntityQuery m_DeletedPaletteInstanceQuery;
        private EntityArchetype m_PaletteInstanceArchetype;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Creates or updates a Palette Instance Entity.
        /// </summary>
        /// <param name="prefabEntity">Palette Prefab entity.</param>
        /// <returns>Instance Entity for palette.</returns>
        public Entity GetOrCreatePaletteInstanceEntity(Entity prefabEntity)
        {
            if (!EntityManager.TryGetBuffer(prefabEntity, isReadOnly: true, out DynamicBuffer<SwatchData> swatchDatas) ||
                swatchDatas.Length < 2)
            {
                return Entity.Null;
            }

            if (!m_PaletteInstanceMap.ContainsKey(prefabEntity))
            {
                Entity paletteInstanceEntity = EntityManager.CreateEntity(m_PaletteInstanceArchetype);
                EntityManager.SetComponentData(paletteInstanceEntity, new PrefabRef(prefabEntity));
                DynamicBuffer<Swatch> swatches = EntityManager.GetBuffer<Swatch>(paletteInstanceEntity, isReadOnly: false);
                for (int i = 0; i < swatchDatas.Length; i++)
                {
                    swatches.Add(new Swatch(swatchDatas[i]));
                }

                EntityManager.AddComponent<Updated>(paletteInstanceEntity);
                m_PaletteInstanceMap.Add(prefabEntity, paletteInstanceEntity);
                return paletteInstanceEntity;
            }
            else
            {
                return m_PaletteInstanceMap[prefabEntity];
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_PaletteInstanceMap = new NativeHashMap<Entity, Entity>(0, Allocator.Persistent);

            m_PaletteInstanceArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Swatch>(), ComponentType.ReadWrite<PrefabRef>());

            m_UpdatedPaletteInstanceQuery = SystemAPI.QueryBuilder()
                   .WithAll<Updated, Swatch, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            m_DeletedPaletteInstanceQuery = SystemAPI.QueryBuilder()
                   .WithAll<Deleted, Swatch, PrefabRef>()
                   .Build();

            RequireAnyForUpdate(m_UpdatedPaletteInstanceQuery, m_DeletedPaletteInstanceQuery);

            m_Log.Info($"{nameof(PaletteInstanceManagerSystem)}.{nameof(OnCreate)} Created.");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!m_UpdatedPaletteInstanceQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_UpdatedPaletteInstanceQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in entities)
                {
                    if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef) ||
                        !EntityManager.HasBuffer<Swatch>(entity))
                    {
                        continue;
                    }

                    if (m_PaletteInstanceMap.ContainsKey(prefabRef.m_Prefab))
                    {
                        m_PaletteInstanceMap[prefabRef.m_Prefab] = entity;
                    }
                    else
                    {
                        m_PaletteInstanceMap.Add(prefabRef.m_Prefab, entity);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_PaletteInstanceMap.Dispose();
        }
    }
}
