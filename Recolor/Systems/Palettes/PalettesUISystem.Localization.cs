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
    using Recolor.Systems.SelectedInfoPanel;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

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

                SaveSelectedLocalCodes();
            }
        }

        private void AddLocale()
        {
            if (TryGetNewLocaleCode(out string localeCode) &&
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
                SaveSelectedLocalCodes();
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

            for (int i = 0; i < m_LocalizationUIDatas.Value[(int)menuType].Length; i++)
            {
                if (m_LocalizationUIDatas.Value[(int)menuType][i].LocaleCode == GameManager.instance.localizationManager.fallbackLocaleId &&
                    m_UniqueNames.Value.Length > (int)menuType)
                {
                    m_LocalizationUIDatas.Value[(int)menuType][i].LocalizedName = m_UniqueNames.Value[(int)menuType];
                }
                else
                {
                    m_LocalizationUIDatas.Value[(int)menuType][i].LocalizedName = string.Empty;
                }

                m_LocalizationUIDatas.Value[(int)menuType][i].LocalizedDescription = string.Empty;
            }
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

        private void SaveSelectedLocalCodes()
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

        private void RemoveLocale(string localeCode)
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
            SaveSelectedLocalCodes();
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
    }
}
