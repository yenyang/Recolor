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
            if (!m_PrefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefabBase) ||
                prefabBase is null)
            {
                return;
            }

            PrefabID prefabID = prefabBase.GetPrefabID();
            if (m_PalettePreferencePrefabIDsMap.ContainsKey(prefabID))
            {
                PalettePreferencePrefabIDs palettePreferenceData = new PalettePreferencePrefabIDs(palettePrefabEntities);
                m_PalettePreferencePrefabIDsMap[prefabID] = palettePreferenceData;
                palettePreferenceData.LogPalettePreference();
            }
            else
            {
                PalettePreferencePrefabIDs palettePreferenceData = new PalettePreferencePrefabIDs(palettePrefabEntities);
                m_PalettePreferencePrefabIDsMap[prefabID] = palettePreferenceData;
                palettePreferenceData.LogPalettePreference();
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
            if (m_PrefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefabBase) &&
                prefabBase is not null &&
                m_PalettePreferencePrefabIDsMap.ContainsKey(prefabBase.GetPrefabID()))
            {
                palettePrefabEntities = m_PalettePreferencePrefabIDsMap[prefabBase.GetPrefabID()].GetPrefabEntities();
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
            writer.Write(m_PalettePreferencePrefabIDsMap.Count);

            foreach (KeyValuePair<PrefabID, PalettePreferencePrefabIDs> keyValuePair in m_PalettePreferencePrefabIDsMap)
            {
                writer.Write(keyValuePair.Key);
                writer.Write(keyValuePair.Value.PalettePrefabIDs[0]);
                writer.Write(keyValuePair.Value.PalettePrefabIDs[1]);
                writer.Write(keyValuePair.Value.PalettePrefabIDs[2]);
                keyValuePair.Value.LogPalettePreference();
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
                    prefabBase is not null)
                {
                    PrefabID prefabID = prefabBase.GetPrefabID();
                    m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(OnGameLoadingComplete)} prefabID is {prefabID}.");
                    PrefabID[] prefabIDs = new PrefabID[3] { default, default, default };
                    for (int i = 0; i < Math.Max(prefabIDs.Length, keyValuePair.Value.PalettePrefabIDs.Length); i++)
                    {
                        if (m_PrefabSystem.TryGetPrefab(keyValuePair.Value.PalettePrefabIDs[i], out PrefabBase palettePrefabBase) &&
                            palettePrefabBase is not null)
                        {
                            prefabIDs[i] = palettePrefabBase.GetPrefabID();
                        }
                    }

                    if (!m_PalettePreferencePrefabIDsMap.ContainsKey(prefabID))
                    {
                       PalettePreferencePrefabIDs preferencePrefabIDs = new PalettePreferencePrefabIDs(prefabIDs);
                       m_PalettePreferencePrefabIDsMap.Add(prefabID, preferencePrefabIDs);
                       preferencePrefabIDs.LogPalettePreference();
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
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

            public PalettePreferencePrefabIDs(Entity[] palettePrefabEntities)
            {
                m_PrefabIDs = new PrefabID[3] { default, default, default };
                for (int i = 0; i < Math.Max(palettePrefabEntities.Length, m_PrefabIDs.Length); i++)
                {
                    m_PrefabIDs[i] = ConvertToPrefabID(palettePrefabEntities[i]);
                }
            }

            public PrefabID[] PalettePrefabIDs => m_PrefabIDs;

            public void LogPalettePreference()
            {
                Mod.Instance.Log.Debug($"{nameof(PalettePreferencePrefabIDs)}.{nameof(LogPalettePreference)} Palette Prefab IDs [{m_PrefabIDs[0]}, {m_PrefabIDs[1]}, {m_PrefabIDs[2]}]");
            }

            public Entity[] GetPrefabEntities()
            {
                Entity[] prefabEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
                for (int i = 0; i < Math.Max(m_PrefabIDs.Length, prefabEntities.Length); i++)
                {
                    PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
                    if (prefabSystem.TryGetPrefab(m_PrefabIDs[i], out PrefabBase prefabBase) &&
                        prefabBase is not null &&
                        prefabSystem.TryGetEntity(prefabBase, out Entity prefabEntity))
                    {
                        prefabEntities[i] = prefabEntity;
                    }
                }

                Mod.Instance.Log.Debug($"{nameof(PalettePreferencePrefabIDs)}.{nameof(GetPrefabEntities)} Palette Prefab Entities [{prefabEntities[0].Index}:{prefabEntities[0].Version}, {prefabEntities[1].Index}:{prefabEntities[1].Version}, {prefabEntities[2].Index}:{prefabEntities[2].Version}]");
                return prefabEntities;
            }

            private PrefabID ConvertToPrefabID(Entity prefabEntity)
            {
                PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
                if (prefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefabBase) &&
                    prefabBase is not null)
                {
                    PrefabID prefabID = prefabBase.GetPrefabID();
                    Mod.Instance.Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(ConvertToPrefabID)} Prefab ID: {prefabID}");
                    return prefabID;
                }

                return default;
            }
        }
    }
}
