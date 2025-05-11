// <copyright file="AddPalettePrefabsSystem.cs" company="Yenyang's Mods. MIT License">
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
    using Recolor.Systems.SelectedInfoPanel;
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
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_SIPColorFieldsSystem = World.GetExistingSystemManaged<SIPColorFieldsSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<PalettesUISystem>();
            m_ModInstallPath = Path.Combine(Mod.Instance.InstallPath, ".PalettePrefabs");
            m_Prefabs = new List<PrefabBase>();
            m_Entities = new Dictionary<PrefabBase, Entity>();
            m_Log.Info($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnCreate)} Created.");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            string[] directories = Directory.GetDirectories(m_UISystem.PalettePrefabsModsDataFolder);
            foreach (string directory in directories)
            {
                string[] filePaths = Directory.GetFiles(directory);
                for (int i = 0; i < filePaths.Length; i++)
                {
                    string fileName = filePaths[i].Remove(0, m_UISystem.PalettePrefabsModsDataFolder.Length);
                    m_Log.Debug($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnUpdate)} Processing {fileName}.");
                    if (fileName.Contains(nameof(PalettePrefab)))
                    {
                        try
                        {
                            using StreamReader reader = new StreamReader(new FileStream(filePaths[i], FileMode.Open));
                            {
                                string entireFile = reader.ReadToEnd();
                                PalettePrefabSerializeFormat palettePrefabSerializeFormat = JsonConvert.DeserializeObject<PalettePrefabSerializeFormat>(entireFile);
                                if (palettePrefabSerializeFormat is null)
                                {
                                    continue;
                                }

                                PalettePrefab palettePrefab = ScriptableObject.CreateInstance<PalettePrefab>();
                                palettePrefabSerializeFormat.AssignValuesToPrefab(ref palettePrefab);
                                if (palettePrefab is not null &&
                                   !m_Prefabs.Contains(palettePrefab) &&
                                    m_PrefabSystem.AddPrefab(palettePrefab) &&
                                    m_PrefabSystem.TryGetEntity(palettePrefab, out Entity palettePrefabEntity))
                                {
                                    palettePrefab.Initialize(EntityManager, palettePrefabEntity);
                                    m_Prefabs.Add(palettePrefab);
                                    m_Entities.Add(palettePrefab, palettePrefabEntity);
                                    m_Log.Info($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnUpdate)} Sucessfully imported and partially initialized {nameof(PalettePrefab)}:{palettePrefab.name}.");

                                    ImportLocalizationFiles(directory);
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
            }

            string[] subcategoryDirectories = Directory.GetDirectories(m_UISystem.SubcategoryPrefabsFolder);
            foreach (string directory in subcategoryDirectories)
            {
                string[] filePaths = Directory.GetFiles(directory);
                for (int i = 0; i < filePaths.Length; i++)
                {
                    string fileName = filePaths[i].Remove(0, m_UISystem.SubcategoryPrefabsFolder.Length);
                    m_Log.Debug($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnUpdate)} Processing {fileName}.");
                    if (fileName.Contains(nameof(PaletteSubCategoryPrefab)))
                    {
                        try
                        {
                            using StreamReader reader = new StreamReader(new FileStream(filePaths[i], FileMode.Open));
                            {
                                string entireFile = reader.ReadToEnd();
                                PaletteSubcategoryPrefabSerializeFormat paletteSubcategoryPrefabSerializeFormat = JsonConvert.DeserializeObject<PaletteSubcategoryPrefabSerializeFormat>(entireFile);
                                if (paletteSubcategoryPrefabSerializeFormat is null)
                                {
                                    continue;
                                }

                                PaletteSubCategoryPrefab subcategoryPrefab = ScriptableObject.CreateInstance<PaletteSubCategoryPrefab>();
                                paletteSubcategoryPrefabSerializeFormat.AssignValuesToPrefab(ref subcategoryPrefab);
                                if (subcategoryPrefab is not null &&
                                   !m_Prefabs.Contains(subcategoryPrefab) &&
                                    m_PrefabSystem.AddPrefab(subcategoryPrefab) &&
                                    m_PrefabSystem.TryGetEntity(subcategoryPrefab, out Entity palettePrefabEntity))
                                {
                                    subcategoryPrefab.Initialize(EntityManager, palettePrefabEntity);
                                    m_Prefabs.Add(subcategoryPrefab);
                                    m_Log.Info($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnUpdate)} Sucessfully imported and partially initialized {nameof(PaletteSubCategoryPrefab)}:{subcategoryPrefab.name}.");
                                    ImportLocalizationFiles(directory);
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            m_Log.Error($"{nameof(AddPalettePrefabsSystem)}.{nameof(OnUpdate)} Could not create or initialize prefab {nameof(PaletteSubCategoryPrefab)}:{fileName}. Encountered Exception: {ex}.");
                        }
                    }

                    // Also handle other prefab types.
                }
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
                m_SIPColorFieldsSystem.UpdatePalettes();
            }
        }

        private void ImportLocalizationFiles(string folderPath)
        {
            if (!System.IO.Directory.Exists(Path.Combine(folderPath, "l10n")))
            {
                return;
            }

            string[] filePaths = Directory.GetFiles(Path.Combine(folderPath, "l10n"));
            for (int i = 0; i < filePaths.Length; i++)
            {
                string fileName = filePaths[i].Remove(0, folderPath.Length + "\\l10n\\".Length);
                string localeId = fileName.Substring(0, fileName.Length - ".json".Length);
                m_Log.Debug($"{nameof(AddPalettePrefabsSystem)}.{nameof(ImportLocalizationFiles)} found json for {localeId}");
                try
                {
                    if (GameManager.instance.localizationManager.SupportsLocale(localeId))
                    {
                        using StreamReader reader = new StreamReader(new FileStream(filePaths[i], FileMode.Open));
                        {
                            string entireFile = reader.ReadToEnd();
                            Colossal.Json.Variant varient = Colossal.Json.JSON.Load(entireFile);
                            Dictionary<string, string> translations = varient.Make<Dictionary<string, string>>();
                            GameManager.instance.localizationManager.AddSource(localeId, new MemorySource(translations));
                            m_Log.Debug($"{nameof(AddPalettePrefabsSystem)}.{nameof(ImportLocalizationFiles)} sucessfully imported localization files for {localeId} from this folder: {folderPath}.");
                        }
                    }
                    else
                    {
                        m_Log.Debug($"{nameof(AddPalettePrefabsSystem)}.{nameof(ImportLocalizationFiles)} {localeId} is not supported");
                    }
                }
                catch (Exception ex)
                {
                    m_Log.Error($"{nameof(AddPalettePrefabsSystem)}.{nameof(ImportLocalizationFiles)} Could not import localization file. Encountered Exception: {ex}. ");
                }
            }
        }
    }
}
