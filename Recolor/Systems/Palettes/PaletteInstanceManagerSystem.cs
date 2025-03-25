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
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Domain.Palette.Prefabs;
    using Recolor.Systems.SelectedInfoPanel;
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
        private EntityQuery m_AssignedPaletteQuery;
        private EntityArchetype m_PaletteInstanceArchetype;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;
        private PrefabSystem m_PrefabSystem;
        private ModificationEndBarrier m_Barrier;

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
                m_Log.Debug($"{nameof(PaletteInstanceManagerSystem)}.{nameof(GetOrCreatePaletteInstanceEntity)} Created Palette Instance Entity {paletteInstanceEntity.Index}.{paletteInstanceEntity.Version}.");
                return paletteInstanceEntity;
            }
            else
            {
                return m_PaletteInstanceMap[prefabEntity];
            }
        }

        /// <summary>
        /// Tries to get palette instance entity from dictionary.
        /// </summary>
        /// <param name="prefabEntity">Palette Prefab entity.</param>
        /// <param name="instanceEntity">Palette instance entity.</param>
        /// <returns>True if found, false if not.</returns>
        public bool TryGetPaletteInstanceEntity(Entity prefabEntity, out Entity instanceEntity)
        {
            instanceEntity = Entity.Null;
            if (!m_PaletteInstanceMap.ContainsKey(prefabEntity))
            {
                return false;
            }
            else
            {
                instanceEntity = m_PaletteInstanceMap[prefabEntity];
                return true;
            }
        }

        /// <summary>
        /// Removes prefab entity from map.
        /// </summary>
        /// <param name="prefabEntity">Palette Prefab Entity.</param>
        public void RemoveFromMap(Entity prefabEntity)
        {
            if (m_PaletteInstanceMap.ContainsKey(prefabEntity))
            {
                m_PaletteInstanceMap.Remove(prefabEntity);
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            m_SIPColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();
            m_PaletteInstanceMap = new NativeHashMap<Entity, Entity>(0, Allocator.Persistent);

            m_PaletteInstanceArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Swatch>(), ComponentType.ReadWrite<PrefabRef>());

            m_UpdatedPaletteInstanceQuery = SystemAPI.QueryBuilder()
                   .WithAll<Updated, Swatch, PrefabRef>()
                   .WithNone<Deleted>()
                   .Build();

            m_DeletedPaletteInstanceQuery = SystemAPI.QueryBuilder()
                   .WithAll<Deleted, Swatch, PrefabRef>()
                   .Build();

            m_AssignedPaletteQuery = SystemAPI.QueryBuilder()
                   .WithAll<AssignedPalette, MeshColor>()
                   .WithNone<Temp, Deleted, Plant>()
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
                        UpdatePaletteInstance(entity, prefabRef.m_Prefab);
                    }
                    else
                    {
                        m_PaletteInstanceMap.Add(prefabRef.m_Prefab, entity);
                    }
                }
            }

            if (!m_DeletedPaletteInstanceQuery.IsEmptyIgnoreFilter)
            {
                NativeArray<Entity> entities = m_DeletedPaletteInstanceQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    DeletePaletteInstance(entities[i]);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_PaletteInstanceMap.Dispose();
        }

        private void UpdatePaletteInstance(Entity paletteInstanceEntity, Entity prefabEntity)
        {
            m_Log.Debug($"{nameof(PaletteInstanceManagerSystem)}.{nameof(UpdatePaletteInstance)} paletteInstanceEntity {paletteInstanceEntity.Index}:{paletteInstanceEntity.Version} prefabEntity {prefabEntity.Index}:{prefabEntity.Version}");
            if (!EntityManager.TryGetBuffer(prefabEntity, isReadOnly: true, out DynamicBuffer<SwatchData> swatchDatas) ||
                swatchDatas.Length < 2)
            {
                return;
            }

            DynamicBuffer<Swatch> swatches = EntityManager.GetBuffer<Swatch>(paletteInstanceEntity, isReadOnly: false);
            swatches.Clear();
            for (int i = 0; i < swatchDatas.Length; i++)
            {
                swatches.Add(new Swatch(swatchDatas[i]));
            }

            m_Log.Debug($"{nameof(PaletteInstanceManagerSystem)}.{nameof(UpdatePaletteInstance)} Updated Swatches.");

            // This updates colors of entities with the updated assigned palette.
            NativeArray<Entity> entities = m_AssignedPaletteQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            for (int i = 0; i < entities.Length; i++)
            {
                if (!EntityManager.TryGetBuffer(entities[i], isReadOnly: true, out DynamicBuffer<AssignedPalette> assignedPalette))
                {
                    continue;
                }

                for (int j = 0; j < assignedPalette.Length; j++)
                {
                    if (assignedPalette[j].m_PaletteInstanceEntity == paletteInstanceEntity)
                    {
                        m_Log.Debug($"{nameof(PaletteInstanceManagerSystem)}.{nameof(UpdatePaletteInstance)} Adding batches updated to entity {entities[i].Index}:{entities[i].Version}.");
                        buffer.AddComponent<BatchesUpdated>(entities[i]);
                        m_SIPColorFieldsSystem.AddBatchesUpdatedToSubElements(entities[i], buffer);
                        if (m_SIPColorFieldsSystem.CurrentEntity == entities[i])
                        {
                            m_SIPColorFieldsSystem.ResetPreviouslySelectedEntity();
                        }

                        break;
                    }
                }
            }
        }

        private void DeletePaletteInstance(Entity paletteInstanceEntity)
        {
            m_Log.Debug($"{nameof(PaletteInstanceManagerSystem)}.{nameof(DeletePaletteInstance)} paletteInstanceEntity {paletteInstanceEntity.Index}:{paletteInstanceEntity.Version}.");

            // This removed assigned palettes associated with paletteInstance entity.
            NativeArray<Entity> entities = m_AssignedPaletteQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            for (int i = 0; i < entities.Length; i++)
            {
                if (!EntityManager.TryGetBuffer(entities[i], isReadOnly: true, out DynamicBuffer<AssignedPalette> assignedPalettes))
                {
                    continue;
                }

                DynamicBuffer<AssignedPalette> newAssignedPalettes = buffer.SetBuffer<AssignedPalette>(entities[i]);
                int removed = 0;
                bool foundAssignedPalette = false;
                for (int j = 0; j < assignedPalettes.Length; j++)
                {
                    if (assignedPalettes[j].m_PaletteInstanceEntity == paletteInstanceEntity)
                    {
                        foundAssignedPalette = true;
                        removed++;
                        m_SIPColorFieldsSystem.ResetSingleInstanceByChannel(assignedPalettes[j].m_Channel, entities[i], buffer);
                    }
                    else
                    {
                        newAssignedPalettes.Add(assignedPalettes[j]);
                    }
                }

                if (foundAssignedPalette && m_SIPColorFieldsSystem.CurrentEntity == entities[i])
                {
                    m_SIPColorFieldsSystem.ResetPreviouslySelectedEntity();
                }

                if (removed == assignedPalettes.Length)
                {
                    buffer.RemoveComponent<AssignedPalette>(entities[i]);
                }
            }
        }
    }
}
