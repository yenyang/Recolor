﻿// <copyright file="AddPalettePrefabsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Colossal.Entities;
    using Colossal.Json;
    using Colossal.Localization;
    using Colossal.Logging;
    using Game;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Newtonsoft.Json;
    using Recolor.Domain.Palette.Prefabs;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// System for adding new prefabs for custom water sources.
    /// </summary>
    public partial class AddPalettePrefabsSystem : GameSystemBase
    {
        private PrefabSystem m_PrefabSystem;
        private PalettesUISystem m_UISystem;
        private ILog m_Log;
        private string m_ModInstallPath;
        private List<PrefabBase> m_Prefabs;
        private Dictionary<PrefabBase, Entity> m_Entities;
        private bool m_FullyInitialized;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<PalettesUISystem>();
            m_ModInstallPath = Path.Combine(Mod.Instance.InstallPath, ".PalettePrefabs");
            m_Prefabs = new List<PrefabBase>();
            m_Entities = new Dictionary<PrefabBase, Entity>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            string[] filePaths = Directory.GetFiles(m_UISystem.ModsDataFolder);
            for (int i = 0; i < filePaths.Length; i++)
            {
                string fileName = filePaths[i].Remove(0, m_UISystem.ModsDataFolder.Length);
                m_Log.Debug($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnUpdate)} Processing {fileName}.");
                if (fileName.Contains(nameof(PalettePrefab)))
                {
                    try
                    {
                        using StreamReader reader = new StreamReader(new FileStream(filePaths[i], FileMode.Open));
                        {
                            string entireFile = reader.ReadToEnd();
                            PalettePrefab palettePrefab = JsonConvert.DeserializeObject<PalettePrefab>(entireFile);
                            if (palettePrefab is not null &&
                               !m_Prefabs.Contains(palettePrefab) &&
                                m_PrefabSystem.AddPrefab(palettePrefab) &&
                                m_PrefabSystem.TryGetEntity(palettePrefab, out Entity palettePrefabEntity))
                            {
                                palettePrefab.Initialize(EntityManager, palettePrefabEntity);
                                m_Prefabs.Add(palettePrefab);
                                m_Entities.Add(palettePrefab, palettePrefabEntity);
                                m_Log.Info($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnUpdate)} Sucessfully imported and partially initialized {nameof(PalettePrefab)}:{nameof(palettePrefab.name)}.");
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        m_Log.Error($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnUpdate)} Could not create or initialize prefab {nameof(PalettePrefab)}:{fileName}. Encountered Exception: {ex}.");
                    }
                }

                // Also handle other prefab types.
            }

            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode.IsGameOrEditor() &&
               !m_FullyInitialized)
            {
                foreach (KeyValuePair<PrefabBase, Entity> keyValuePair in m_Entities)
                {
                    keyValuePair.Key.LateInitialize(EntityManager, keyValuePair.Value);
                }

                m_FullyInitialized = true;
            }
        }
    }
}
