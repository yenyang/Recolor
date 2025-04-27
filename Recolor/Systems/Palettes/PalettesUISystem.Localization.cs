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
    /// A UI System for Palettes and Swatches.
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

                    newLocalizationUIDatas[m_LocalizationUIDatas.Value[i].Length] = new LocalizationUIData(localeCode, m_UniqueNames.Value[i], string.Empty);
                    m_LocalizationUIDatas.Value[i] = newLocalizationUIDatas;
                }

                m_LocalizationUIDatas.Binding.TriggerUpdate();
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
    }
}
