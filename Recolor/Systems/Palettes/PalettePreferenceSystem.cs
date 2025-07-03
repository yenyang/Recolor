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
            reader.Read(out int version);

            reader.Read(out int count);

            for (int i = 0; i < count; i++)
            {
                reader.Read(out PrefabID prefabEntityID);
                m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(Deserialize)} PrefabID: {prefabEntityID}");
                List<PalettePreference> palettePreferences = new List<PalettePreference>();

                if (version >= 2)
                {
                    reader.Read(out int preferenceCount);

                    for (int j = 0; j < preferenceCount; j++)
                    {
                        reader.Read(out int channel);
                        reader.Read(out PrefabID prefabID);
                        if (j < 3 &&
                            channel >= 0 &&
                            channel <= 2)
                        {
                            palettePreferences.Add(new PalettePreference(channel, prefabID));
                        }
                    }

                    if (!m_PalettePreferencePrefabIDsMap.ContainsKey(prefabEntityID) &&
                        palettePreferences.Count > 0 &&
                        palettePreferences.Count <= 3)
                    {
                        PalettePreferencePrefabIDs data = new PalettePreferencePrefabIDs(palettePreferences);
                        m_PalettePreferencePrefabIDsMap.Add(prefabEntityID, data);
                        data.LogPalettePreference();
                    }
                }
                else if (version == 1)
                {
                    m_Log.Info($"{nameof(PalettePreferenceSystem)}.{nameof(Deserialize)} Version 1 detected.");

                    // Version 1 couldn't handle no palette assigned.
                    for (int j = 0; j < 3; j++)
                    {
                        reader.Read(out PrefabID prefabID);
                        palettePreferences.Add(new PalettePreference(j, prefabID));
                    }

                    if (!m_PalettePreferencePrefabIDsMap.ContainsKey(prefabEntityID))
                    {
                        PalettePreferencePrefabIDs data = new PalettePreferencePrefabIDs(palettePreferences);
                        m_PalettePreferencePrefabIDsMap.Add(prefabEntityID, data);
                        data.LogPalettePreference();
                    }

                }
            }
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            m_Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(Serialize)}");

            // Version number
            writer.Write(2);

            // Count of palette preference map
            writer.Write(m_PalettePreferencePrefabIDsMap.Count);

            foreach (KeyValuePair<PrefabID, PalettePreferencePrefabIDs> keyValuePair in m_PalettePreferencePrefabIDsMap)
            {
                writer.Write(keyValuePair.Key);
                writer.Write(keyValuePair.Value.PalettePreferences.Count);
                for (int i = 0; i < keyValuePair.Value.PalettePreferences.Count; i++)
                {
                    writer.Write(keyValuePair.Value.PalettePreferences[i].Channel);
                    writer.Write(keyValuePair.Value.PalettePreferences[i].PrefabID);
                }

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

        private struct PalettePreference
        {
            private int m_Channel;
            private PrefabID m_PrefabID;

            public PalettePreference (int channel, PrefabID prefabID)
            {
                m_Channel = channel;
                m_PrefabID = prefabID;
            }

            public int Channel => m_Channel;

            public PrefabID PrefabID => m_PrefabID;
        }

        private struct PalettePreferencePrefabIDs
        {
            private List<PalettePreference> m_PalettePreferences;

            /// <summary>
            /// Initializes a new instance of the <see cref="PalettePreferencePrefabIDs"/> struct.
            /// </summary>
            /// <param name="palettePrefabIDs">Palette Prefab IDs.</param>
            public PalettePreferencePrefabIDs(List<PalettePreference> palettePreferences)
            {
                m_PalettePreferences = palettePreferences;
            }

            public PalettePreferencePrefabIDs(Entity[] palettePrefabEntities)
            {
                m_PalettePreferences = new List<PalettePreference>();
                for (int i = 0; i < Math.Max(palettePrefabEntities.Length, 3); i++)
                {
                    if (TryConvertToPrefabID(palettePrefabEntities[i], out PrefabID prefabID))
                    {
                        m_PalettePreferences.Add(new PalettePreference(i, prefabID));
                    }
                }
            }

            public List<PalettePreference> PalettePreferences => m_PalettePreferences;

            public void LogPalettePreference()
            {
                foreach (PalettePreference palettePreference in m_PalettePreferences)
                {
                    Mod.Instance.Log.Debug($"{nameof(PalettePreferencePrefabIDs)}.{nameof(LogPalettePreference)} Palette Prefernce Channel: {palettePreference.Channel} PrefabID: {palettePreference.PrefabID}]");
                }
            }

            public Entity[] GetPrefabEntities()
            {
                Entity[] prefabEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
                for (int i = 0; i < Math.Max(m_PalettePreferences.Count, prefabEntities.Length); i++)
                {
                    PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
                    if (prefabSystem.TryGetPrefab(m_PalettePreferences[i].PrefabID, out PrefabBase prefabBase) &&
                        prefabBase is not null &&
                        prefabSystem.TryGetEntity(prefabBase, out Entity prefabEntity))
                    {
                        prefabEntities[i] = prefabEntity;
                    }
                }

                Mod.Instance.Log.Debug($"{nameof(PalettePreferencePrefabIDs)}.{nameof(GetPrefabEntities)} Palette Prefab Entities [{prefabEntities[0].Index}:{prefabEntities[0].Version}, {prefabEntities[1].Index}:{prefabEntities[1].Version}, {prefabEntities[2].Index}:{prefabEntities[2].Version}]");
                return prefabEntities;
            }

            private bool TryConvertToPrefabID(Entity prefabEntity, out PrefabID prefabID)
            {
                PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
                if (prefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefabBase) &&
                    prefabBase is not null)
                {
                    prefabID = prefabBase.GetPrefabID();
                    Mod.Instance.Log.Debug($"{nameof(PalettePreferenceSystem)}.{nameof(ConvertToPrefabID)} Prefab ID: {prefabID}");
                    return true;
                }

                return false;
            }
        }
    }
}
