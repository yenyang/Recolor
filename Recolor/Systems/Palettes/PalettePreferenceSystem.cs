// <copyright file="PalettePreferenceSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System;
    using System.Collections.Generic;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Prefabs;
    using Unity.Entities;

    /// <summary>
    /// Saves preferences for palette choices for specific prefabs.
    /// </summary>
    public partial class PalettePreferenceSystem : GameSystemBase, ISerializable, IDefaultSerializable
    {
        private Dictionary<Entity, PalettePreferenceData> m_PalettePreferenceMap;
        private Dictionary<PrefabID, PalettePreferencePrefabIDs> m_PalettePreferencePrefabIDsMap;
        private ILog m_Log;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Registers a new palette preference for a prefab entity.
        /// </summary>
        /// <param name="prefabEntity">Prefab entity of object beign placed.</param>
        /// <param name="palettePrefabEntities">Choose palette prefab entities.</param>
        public void RegisterPalettePreference(Entity prefabEntity, Entity[] palettePrefabEntities)
        {
            if (m_PalettePreferenceMap.ContainsKey(prefabEntity))
            {
                PalettePreferenceData palettePreferenceData = new PalettePreferenceData(palettePrefabEntities);
                m_PalettePreferenceMap[prefabEntity] = palettePreferenceData;
                palettePreferenceData.LogPalettePreference();
                ConvertToPrefabID(prefabEntity);
                for (int i = 0; i < palettePrefabEntities.Length; i++)
                {
                    ConvertToPrefabID(palettePrefabEntities[i]);
                }
            }
            else
            {
                PalettePreferenceData palettePreferenceData = new PalettePreferenceData(palettePrefabEntities);
                m_PalettePreferenceMap.Add(prefabEntity, palettePreferenceData);
                palettePreferenceData.LogPalettePreference();
                ConvertToPrefabID(prefabEntity);
                for (int i = 0; i < palettePrefabEntities.Length; i++)
                {
                    ConvertToPrefabID(palettePrefabEntities[i]);
                }
            }
        }

        /// <summary>
        /// Tries to get the palette prefab entities for this prefab entity.
        /// </summary>
        /// <param name="prefabEntity">Prefab entity for placement object.</param>
        /// <param name="palettePrefabEntities">palette prefab entities.</param>
        /// <returns>True if found, False if not.</returns>
        public bool TryGetPalettePrefabPreference(Entity prefabEntity, out Entity[] palettePrefabEntities)
        {
            if (m_PalettePreferenceMap.ContainsKey(prefabEntity))
            {
                palettePrefabEntities = m_PalettePreferenceMap[prefabEntity].PalettePrefabEntities;
                return true;
            }
            else
            {
                palettePrefabEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
                return false;
            }
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(Deserialize)}");

            // Version number
            reader.Read(out int _);

            reader.Read(out int count);

            for (int i = 0; i < count; i++)
            {
                reader.Read(out PrefabID prefabEntityID);
                m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(Deserialize)} PrefabID: {prefabEntityID}");
                PrefabID[] prefabIDs = new PrefabID[3] { default, default, default };
                reader.Read(out prefabIDs[0]);
                reader.Read(out prefabIDs[1]);
                reader.Read(out prefabIDs[2]);
                if (!m_PalettePreferencePrefabIDsMap.ContainsKey(prefabEntityID))
                {
                    PalettePreferencePrefabIDs data = new PalettePreferencePrefabIDs(prefabIDs);
                    m_PalettePreferencePrefabIDsMap.Add(prefabEntityID, data);
                    data.LogPalettePreference();
                }
            }
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(Serialize)}");

            // Version number
            writer.Write(1);

            // Count of palette preference map
            writer.Write(m_PalettePreferenceMap.Count);

            foreach (KeyValuePair<Entity, PalettePreferenceData> keyValuePair in m_PalettePreferenceMap)
            {
                writer.Write(ConvertToPrefabID(keyValuePair.Key));
                writer.Write(ConvertToPrefabID(keyValuePair.Value.PalettePrefabEntities[0]));
                writer.Write(ConvertToPrefabID(keyValuePair.Value.PalettePrefabEntities[1]));
                writer.Write(ConvertToPrefabID(keyValuePair.Value.PalettePrefabEntities[2]));
            }
        }

        /// <inheritdoc/>
        public void SetDefaults(Context context)
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            m_PalettePreferenceMap = new Dictionary<Entity, PalettePreferenceData>();
            m_PalettePreferencePrefabIDsMap = new Dictionary<PrefabID, PalettePreferencePrefabIDs>();

            Enabled = false;
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(PalettePreferenceSystem)}.{nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(OnGameLoadingComplete)}");
            foreach (KeyValuePair<PrefabID, PalettePreferencePrefabIDs> keyValuePair in m_PalettePreferencePrefabIDsMap)
            {
                if (m_PrefabSystem.TryGetPrefab(keyValuePair.Key, out PrefabBase prefabBase) &&
                    prefabBase is not null &&
                    m_PrefabSystem.TryGetEntity(prefabBase, out Entity prefabEntity))
                {
                    m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(OnGameLoadingComplete)} prefabBase is {prefabBase.name}.");
                    Entity[] prefabEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
                    for (int i = 0; i < prefabEntities.Length; i++)
                    {
                        if (m_PrefabSystem.TryGetPrefab(keyValuePair.Value.PalettePrefabIDs[i], out PrefabBase palettePrefabBase) &&
                            palettePrefabBase is not null &&
                            m_PrefabSystem.TryGetEntity(prefabBase, out Entity palettePrefabEntity))
                        {
                            prefabEntities[i] = palettePrefabEntity;
                        }
                    }

                    if (prefabEntity != Entity.Null &&
                       !m_PalettePreferenceMap.ContainsKey(prefabEntity) &&
                       (prefabEntities[0] != Entity.Null ||
                        prefabEntities[1] != Entity.Null ||
                        prefabEntities[2] != Entity.Null))
                    {
                        PalettePreferenceData palettePreferenceData = new PalettePreferenceData(prefabEntities);
                        m_PalettePreferenceMap.Add(prefabEntity, palettePreferenceData);
                        palettePreferenceData.LogPalettePreference();
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            m_PalettePreferenceMap.Clear();
            m_PalettePreferencePrefabIDsMap.Clear();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            return;
        }

        private PrefabID ConvertToPrefabID(Entity prefabEntity)
        {
            if (m_PrefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefabBase) &&
                prefabBase is not null)
            {
                PrefabID prefabID = prefabBase.GetPrefabID();
                m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(ConvertToPrefabID)} Prefab ID: {prefabID}");
                return prefabID;
            }

            return default;
        }

        private struct PalettePreferenceData
        {
            private Entity[] m_PalettePrefabEntities;

            /// <summary>
            /// Initializes a new instance of the <see cref="PalettePreferenceData"/> struct.
            /// </summary>
            /// <param name="palettePrefabEntities">Palette Prefab Entities.</param>
            public PalettePreferenceData(Entity[] palettePrefabEntities)
            {
                m_PalettePrefabEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
                for (int i = 0; i < Math.Max(palettePrefabEntities.Length, m_PalettePrefabEntities.Length); i++)
                {
                    m_PalettePrefabEntities[i] = palettePrefabEntities[i];
                }
            }

            public Entity[] PalettePrefabEntities => m_PalettePrefabEntities;

            public void LogPalettePreference()
            {
                Mod.Instance.Log.Debug($"{nameof(PalettePreferenceData)}.{nameof(LogPalettePreference)} Palette Prefab Entities [{m_PalettePrefabEntities[0].Index}:{m_PalettePrefabEntities[0].Version}, {m_PalettePrefabEntities[1].Index}:{m_PalettePrefabEntities[1].Version}, {m_PalettePrefabEntities[2].Index}:{m_PalettePrefabEntities[2].Version}]");
            }
        }

        private struct PalettePreferencePrefabIDs
        {
            private PrefabID[] m_PrefabIDs;

            /// <summary>
            /// Initializes a new instance of the <see cref="PalettePreferencePrefabIDs"/> struct.
            /// </summary>
            /// <param name="palettePrefabIDs">Palette Prefab IDs.</param>
            public PalettePreferencePrefabIDs(PrefabID[] palettePrefabIDs)
            {
                m_PrefabIDs = new PrefabID[3] { default, default, default };
                for (int i = 0; i < Math.Max(palettePrefabIDs.Length, m_PrefabIDs.Length); i++)
                {
                    m_PrefabIDs[i] = palettePrefabIDs[i];
                }
            }

            public PrefabID[] PalettePrefabIDs => m_PrefabIDs;

            public void LogPalettePreference()
            {
                Mod.Instance.Log.Debug($"{nameof(PalettePreferenceData)}.{nameof(LogPalettePreference)} Palette Prefab IDs [{m_PrefabIDs[0]}, {m_PrefabIDs[1]}, {m_PrefabIDs[2]}]");
            }
        }
    }
}
