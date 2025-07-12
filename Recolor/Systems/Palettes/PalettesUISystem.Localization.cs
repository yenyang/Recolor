// <copyright file="PalettesUISystem.Localization.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Colossal.Entities;
    using Colossal.IO.AssetDatabase;
    using Colossal.Json;
    using Colossal.Localization;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Game.Tools;
    using Game.UI.InGame;
    using Newtonsoft.Json;
    using Recolor.Domain.Palette;
    using Recolor.Domain.Palette.Prefabs;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Handles the localization portions for Palettes and Subcategories.
    /// </summary>
    public partial class PalettesUISystem : ExtendedUISystemBase
    {
        private void ChangeLocaleCode(string localeCode, int index)
        {
            if (GameManager.instance.localizationManager.GetSupportedLocales().Contains(localeCode) &&
                m_LocalizationUIDatas.Value.Length > 0 &&
                index >= 0)
            {
                for (int i = 0; i < m_LocalizationUIDatas.Value.Length; i++)
                {
                    if (m_LocalizationUIDatas.Value[i].Length > index)
                    {
                        m_LocalizationUIDatas.Value[i][index].LocaleCode = localeCode;
                        m_LocalizationUIDatas.Binding.TriggerUpdate();
                    }
                }

                SaveSelectedLocaleCodes();
            }
        }

        private void AddLocale(string localeCode, bool saveSelectedLocaleCodes = false)
        {
            if (!IsLocaleCodeSelected(localeCode) &&
                m_LocalizationUIDatas.Value.Length > 0)
            {
                for (int i = 0; i < m_LocalizationUIDatas.Value.Length; i++)
                {
                    LocalizationUIData[] newLocalizationUIDatas = new LocalizationUIData[m_LocalizationUIDatas.Value[i].Length + 1];
                    for (int j = 0; j < m_LocalizationUIDatas.Value[i].Length; j++)
                    {
                        newLocalizationUIDatas[j] = m_LocalizationUIDatas.Value[i][j];
                    }

                    if (localeCode == GameManager.instance.localizationManager.fallbackLocaleId &&
                        m_UniqueNames.Value.Length > i)
                    {
                        newLocalizationUIDatas[m_LocalizationUIDatas.Value[i].Length] = new LocalizationUIData(localeCode, m_UniqueNames.Value[i], string.Empty);
                    }
                    else
                    {
                        newLocalizationUIDatas[m_LocalizationUIDatas.Value[i].Length] = new LocalizationUIData(localeCode, string.Empty, string.Empty);
                    }

                    m_LocalizationUIDatas.Value[i] = newLocalizationUIDatas;
                }

                m_LocalizationUIDatas.Binding.TriggerUpdate();
                if (saveSelectedLocaleCodes)
                {
                    SaveSelectedLocaleCodes();
                }
            }
        }

        private void AddLocale()
        {
            if (TryGetNewLocaleCode(out string localeCode))
            {
                AddLocale(localeCode, true);
            }
        }

        private bool TryGetNewLocaleCode(out string localeCode)
        {
            localeCode = string.Empty;
            if (m_LocalizationUIDatas.Value.Length == 0)
            {
                return false;
            }

            if (!IsLocaleCodeSelected(GameManager.instance.localizationManager.activeLocaleId) &&
                GameManager.instance.localizationManager.GetSupportedLocales().Contains(GameManager.instance.localizationManager.activeLocaleId))
            {
                localeCode = GameManager.instance.localizationManager.activeLocaleId;
                return true;
            }

            if (!IsLocaleCodeSelected(GameManager.instance.localizationManager.fallbackLocaleId) &&
                GameManager.instance.localizationManager.GetSupportedLocales().Contains(GameManager.instance.localizationManager.fallbackLocaleId))
            {
                localeCode = GameManager.instance.localizationManager.fallbackLocaleId;
                return true;
            }

            string[] localeCodes = GameManager.instance.localizationManager.GetSupportedLocales();
            for (int i = 0; i < localeCodes.Length; i++)
            {
                if (!IsLocaleCodeSelected(localeCodes[i]))
                {
                    localeCode = localeCodes[i];
                    return true;
                }
            }

            return false;
        }

        private bool IsLocaleCodeSelected(string localeCode)
        {
            if (m_LocalizationUIDatas.Value.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < m_LocalizationUIDatas.Value[0].Length; i++)
            {
                if (m_LocalizationUIDatas.Value[0][i].LocaleCode == localeCode)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetToDefaultLocalizationUIDatas(MenuType menuType)
        {
            if (m_LocalizationUIDatas.Value.Length <= (int)menuType)
            {
                return;
            }

            List<string> localeCodeList = Mod.Instance.Settings.SelectedLocaleCodes.ToList();

            MenuType oppositeMenuType = MenuType.Subcategory;
            if (menuType == MenuType.Subcategory)
            {
                oppositeMenuType = MenuType.Palette;
            }

            // This section goes through the opposite menu type to ensure that if there are locale codes being shown there that they are included in both menus.
            if (m_LocalizationUIDatas.Value.Length > (int)oppositeMenuType)
            {
                for (int i = 0; i < m_LocalizationUIDatas.Value[(int)oppositeMenuType].Length; i++)
                {
                    if (oppositeMenuType == MenuType.Subcategory &&
                        !m_ShowSubcategoryEditorPanel &&
                        !localeCodeList.Contains(m_LocalizationUIDatas.Value[(int)oppositeMenuType][i].LocaleCode))
                    {
                        RemoveLocale(m_LocalizationUIDatas.Value[(int)oppositeMenuType][i].LocaleCode, false);
                    }
                    else if (!localeCodeList.Contains(m_LocalizationUIDatas.Value[(int)oppositeMenuType][i].LocaleCode))
                    {
                        localeCodeList.Add(m_LocalizationUIDatas.Value[(int)oppositeMenuType][i].LocaleCode);
                    }
                }
            }

            string[] localeCodes = localeCodeList.ToArray();

            Dictionary<string, LocalizationUIData> localizationUIData = new Dictionary<string, LocalizationUIData>();

            for (int i = 0; i < localeCodes.Length; i++)
            {
                if (GameManager.instance.localizationManager.GetSupportedLocales().Contains(localeCodes[i]) &&
                    localeCodes[i] == GameManager.instance.localizationManager.fallbackLocaleId &&
                    m_UniqueNames.Value.Length > (int)menuType)
                {
                    localizationUIData.Add(Mod.Instance.Settings.SelectedLocaleCodes[i], new LocalizationUIData(localeCodes[i], m_UniqueNames.Value[(int)menuType], string.Empty));
                }
                else if (GameManager.instance.localizationManager.GetSupportedLocales().Contains(localeCodes[i]))
                {
                    localizationUIData.Add(localeCodes[i], new LocalizationUIData(localeCodes[i], string.Empty, string.Empty));
                }

                m_LocalizationUIDatas.Value[(int)menuType] = localizationUIData.Values.ToArray();
            }

            m_LocalizationUIDatas.Binding.TriggerUpdate();
        }

        private void InitializeLocalizationUIDatas()
        {
            if (m_LocalizationUIDatas.Value.Length != 2)
            {
                m_LocalizationUIDatas.Value = new LocalizationUIData[2][];
            }

            Dictionary<string, LocalizationUIData>[] localizationUIDatas = new Dictionary<string, LocalizationUIData>[2];
            for (int i = 0; i < 2; i++)
            {
                localizationUIDatas[i] = new Dictionary<string, LocalizationUIData>();
                for (int j = 0; j < Mod.Instance.Settings.SelectedLocaleCodes.Length; j++)
                {
                    if (!localizationUIDatas[i].ContainsKey(Mod.Instance.Settings.SelectedLocaleCodes[j]) &&
                        GameManager.instance.localizationManager.GetSupportedLocales().Contains(Mod.Instance.Settings.SelectedLocaleCodes[j]) &&
                        Mod.Instance.Settings.SelectedLocaleCodes[j] == GameManager.instance.localizationManager.fallbackLocaleId &&
                        m_UniqueNames.Value.Length > i)
                    {
                        localizationUIDatas[i].Add(Mod.Instance.Settings.SelectedLocaleCodes[j], new LocalizationUIData(Mod.Instance.Settings.SelectedLocaleCodes[j], m_UniqueNames.Value[i], string.Empty));
                    }
                    else if (!localizationUIDatas[i].ContainsKey(Mod.Instance.Settings.SelectedLocaleCodes[j]) &&
                                GameManager.instance.localizationManager.GetSupportedLocales().Contains(Mod.Instance.Settings.SelectedLocaleCodes[j]))
                    {
                        localizationUIDatas[i].Add(Mod.Instance.Settings.SelectedLocaleCodes[j], new LocalizationUIData(Mod.Instance.Settings.SelectedLocaleCodes[j], string.Empty, string.Empty));
                    }
                }

                m_LocalizationUIDatas.Value[i] = localizationUIDatas[i].Values.ToArray();
            }

            m_LocalizationUIDatas.Binding.TriggerUpdate();
        }

        private void SaveSelectedLocaleCodes()
        {
            if (m_LocalizationUIDatas.Value.Length > 0)
            {
                List<string> localeCodes = new List<string>();
                for (int i = 0; i < m_LocalizationUIDatas.Value[0].Length; i++)
                {
                    if (!localeCodes.Contains(m_LocalizationUIDatas.Value[0][i].LocaleCode))
                    {
                        localeCodes.Add(m_LocalizationUIDatas.Value[0][i].LocaleCode);
                    }
                }

                Mod.Instance.Settings.SelectedLocaleCodes = localeCodes.ToArray();
                Mod.Instance.Settings.ApplyAndSave();
            }
        }

        private void RemoveLocale(string localeCode, bool saveLocaleCodes = false)
        {
            Dictionary<string, LocalizationUIData>[] localizationUIDatas = new Dictionary<string, LocalizationUIData>[m_LocalizationUIDatas.Value.Length];
            for (int i = 0; i < m_LocalizationUIDatas.Value.Length; i++)
            {
                localizationUIDatas[i] = new Dictionary<string, LocalizationUIData>();
                for (int j = 0; j < m_LocalizationUIDatas.Value[i].Length; j++)
                {
                    if (m_LocalizationUIDatas.Value[i][j].LocaleCode != localeCode &&
                        !localizationUIDatas[i].ContainsKey(m_LocalizationUIDatas.Value[i][j].LocaleCode) &&
                        GameManager.instance.localizationManager.GetSupportedLocales().Contains(m_LocalizationUIDatas.Value[i][j].LocaleCode))
                    {
                        localizationUIDatas[i].Add(m_LocalizationUIDatas.Value[i][j].LocaleCode, m_LocalizationUIDatas.Value[i][j]);
                    }
                }

                m_LocalizationUIDatas.Value[i] = localizationUIDatas[i].Values.ToArray();
            }

            m_LocalizationUIDatas.Binding.TriggerUpdate();
            if (saveLocaleCodes)
            {
                SaveSelectedLocaleCodes();
            }
        }

        private void RemoveLocale(string localeCode)
        {
            RemoveLocale(localeCode, true);
        }

        private void ChangeLocalizedName(int menuType, int index, string name)
        {
            if (m_LocalizationUIDatas.Value.Length > menuType &&
                m_LocalizationUIDatas.Value[menuType].Length > index)
            {
                m_LocalizationUIDatas.Value[menuType][index].LocalizedName = name;
                m_LocalizationUIDatas.Binding.TriggerUpdate();
            }
        }

        private void ChangeLocalizedDescription(int menuType, int index, string name)
        {
            if (m_LocalizationUIDatas.Value.Length > menuType &&
                m_LocalizationUIDatas.Value[menuType].Length > index)
            {
                m_LocalizationUIDatas.Value[menuType][index].LocalizedDescription = name;
                m_LocalizationUIDatas.Binding.TriggerUpdate();
            }
        }

        private Dictionary<string, string> CompileLocalizationDictionary(LocalizationUIData localizationUIData, MenuType menuType, string uniqueName)
        {
            string prefix = Mod.Id + ".";
            prefix += menuType == MenuType.Palette ? nameof(MenuType.Palette) : nameof(MenuType.Subcategory);

            return new Dictionary<string, string> { { $"{prefix}.NAME[{uniqueName}]", localizationUIData.LocalizedName }, { $"{prefix}.DESCRIPTION[{uniqueName}]", localizationUIData.LocalizedDescription } };
        }

        private bool TryExportLocalizationFile(string folderPath, string localeCode, Dictionary<string, string> localizationDictionary)
        {
            try
            {
                string json = JSON.Dump(localizationDictionary);
                System.IO.Directory.CreateDirectory(Path.Combine(folderPath, "l10n"));
                File.WriteAllText(Path.Combine(folderPath, "l10n", $"{localeCode}.json"), json);
                return true;
            }
            catch (Exception ex)
            {
                m_Log.Error($"{nameof(PalettesUISystem)}.{nameof(TryExportLocalizationFile)} Could not export localization file. Encountered Exception: {ex}. ");
            }

            return false;
        }

        private bool TryExportLocalizationFile(string folderPath, LocalizationUIData localizationUIData, MenuType menuType, string uniqueName)
        {
            return TryExportLocalizationFile(folderPath, localizationUIData.LocaleCode, CompileLocalizationDictionary(localizationUIData, menuType, uniqueName));
        }

        private void AddLocalization(LocalizationUIData localizationUIData, MenuType menuType, string uniqueName)
        {
            try
            {
                if (GameManager.instance.localizationManager.SupportsLocale(localizationUIData.LocaleCode))
                {
                        Dictionary<string, string> translations = CompileLocalizationDictionary(localizationUIData, menuType, uniqueName);
                        GameManager.instance.localizationManager.AddSource(localizationUIData.LocaleCode, new MemorySource(translations));
                        m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(AddLocalization)} sucessfully imported localization files for {localizationUIData.LocaleCode} and {uniqueName}.");
                }
                else
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(AddLocalization)} {localizationUIData.LocaleCode} is not supported");
                }
            }
            catch (Exception ex)
            {
                m_Log.Error($"{nameof(PalettesUISystem)}.{nameof(AddLocalization)} Could not import localization file. Encountered Exception: {ex}. ");
            }
        }

        private void EditLocalizationFiles(string folderPath, MenuType menuType, string uniqueName)
        {
            if (!System.IO.Directory.Exists(Path.Combine(folderPath, "l10n")))
            {
                for (int j = 0; j < m_LocalizationUIDatas.Value[(int)menuType].Length; j++)
                {
                    m_LocalizationUIDatas.Value[(int)menuType][j].LocalizedName = uniqueName;
                    m_LocalizationUIDatas.Value[(int)menuType][j].LocalizedDescription = string.Empty;
                }

                m_LocalizationUIDatas.Binding.TriggerUpdate();
                return;
            }

            ResetToDefaultLocalizationUIDatas(menuType);

            string[] filePaths = Directory.GetFiles(Path.Combine(folderPath, "l10n"));
            for (int i = 0; i < filePaths.Length; i++)
            {
                string fileName = filePaths[i].Remove(0, folderPath.Length + "\\l10n\\".Length);
                string localeId = fileName.Substring(0, fileName.Length - ".json".Length);
                m_Log.Debug($"{nameof(AddPalettePrefabsSystem)}.{nameof(EditLocalizationFiles)} found json for {localeId}");
                try
                {
                    if (GameManager.instance.localizationManager.SupportsLocale(localeId))
                    {
                        using StreamReader reader = new StreamReader(new FileStream(filePaths[i], FileMode.Open));
                        {
                            string entireFile = reader.ReadToEnd();
                            Colossal.Json.Variant varient = Colossal.Json.JSON.Load(entireFile);
                            Dictionary<string, string> translations = varient.Make<Dictionary<string, string>>();
                            if (!IsLocaleCodeSelected(localeId) &&
                                translations.Count > 0)
                            {
                                AddLocale(localeId);
                            }

                            for (int j = 0; j < m_LocalizationUIDatas.Value[(int)menuType].Length; j++)
                            {
                                if (m_LocalizationUIDatas.Value[(int)menuType][j].LocaleCode == localeId)
                                {
                                    if (translations.ContainsKey(LocaleEN.NameKey(menuType, uniqueName)))
                                    {
                                        m_LocalizationUIDatas.Value[(int)menuType][j].LocalizedName = translations[LocaleEN.NameKey(menuType, uniqueName)];
                                    }

                                    if (translations.ContainsKey(LocaleEN.DescriptionKey(menuType, uniqueName)))
                                    {
                                        m_LocalizationUIDatas.Value[(int)menuType][j].LocalizedDescription = translations[LocaleEN.DescriptionKey(menuType, uniqueName)];
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        m_Log.Debug($"{nameof(AddPalettePrefabsSystem)}.{nameof(EditLocalizationFiles)} {localeId} is not supported");
                    }
                }
                catch (Exception ex)
                {
                    m_Log.Error($"{nameof(AddPalettePrefabsSystem)}.{nameof(EditLocalizationFiles)} Could not edit localization file. Encountered Exception: {ex}. ");
                }

                m_LocalizationUIDatas.Binding.TriggerUpdate();
            }
        }

        private void DeleteLocalizationFiles(string folderPath)
        {
            if (!System.IO.Directory.Exists(Path.Combine(folderPath, "l10n")))
            {
                return;
            }

            string[] filePaths = Directory.GetFiles(Path.Combine(folderPath, "l10n"));
            for (int i = 0; i < filePaths.Length; i++)
            {
                try
                {
                    File.Delete(filePaths[i]);
                }
                catch (Exception e)
                {
                    m_Log.Info($"Could not remove file at {filePaths[i]} encountered exception {e}.");
                }
            }

            try
            {
                Directory.Delete(Path.Combine(folderPath, "l10n"));
            }
            catch (Exception e)
            {
                m_Log.Info($"Could not remove directory at {Path.Combine(folderPath, "l10n")} encountered exception {e}.");
            }
        }
    }
}
