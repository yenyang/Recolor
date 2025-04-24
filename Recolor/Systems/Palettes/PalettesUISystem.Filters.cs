// <copyright file="PalettesUISystem.Filters.cs" company="Yenyang's Mods. MIT License">
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
    using Game.Tools;
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
        private void SetFilter(int filterType)
        {
            PaletteFilterTypeData.PaletteFilterType paletteFilterType = (PaletteFilterTypeData.PaletteFilterType)filterType;
            m_PaletteFilterType.Value = paletteFilterType;

            EntityQuery filterQuery;

            switch (paletteFilterType)
            {
                case PaletteFilterTypeData.PaletteFilterType.None:
                    m_FilterEntities.Value = new PaletteFilterEntityUIData[0];
                    m_FilterEntities.Binding.TriggerUpdate();
                    m_SelectedFilterPrefabEntities.Value = new Entity[0];
                    m_SelectedFilterPrefabEntities.Binding.TriggerUpdate();
                    return;
                case PaletteFilterTypeData.PaletteFilterType.Theme:
                    filterQuery = m_ThemePrefabEntityQuery;
                    break;
                case PaletteFilterTypeData.PaletteFilterType.Pack:
                    filterQuery = m_AssetPackQuery;
                    break;
                case PaletteFilterTypeData.PaletteFilterType.ZoningType:
                    filterQuery = m_ZonePrefabEntityQuery;
                    break;
                default:
                    m_FilterEntities.Value = new PaletteFilterEntityUIData[0];
                    m_FilterEntities.Binding.TriggerUpdate();
                    m_SelectedFilterPrefabEntities.Value = new Entity[0];
                    m_SelectedFilterPrefabEntities.Binding.TriggerUpdate();
                    return;
            }

            NativeArray<Entity> prefabEntities = filterQuery.ToEntityArray(Allocator.Temp);
            PaletteFilterEntityUIData[] paletteFilterEntityUIDatas = new PaletteFilterEntityUIData[prefabEntities.Length];
            for (int i = 0; i < prefabEntities.Length; i++)
            {
                paletteFilterEntityUIDatas[i] = new PaletteFilterEntityUIData(prefabEntities[i], GetLocaleKey(prefabEntities[i], paletteFilterType));
            }

            m_FilterEntities.Value = paletteFilterEntityUIDatas;
            m_FilterEntities.Binding.TriggerUpdate();
            if (paletteFilterEntityUIDatas.Length > 0)
            {
                m_SelectedFilterPrefabEntities.Value = new Entity[1] { paletteFilterEntityUIDatas[0].FilterPrefabEntity };
                m_SelectedFilterPrefabEntities.Binding.TriggerUpdate();
            }
        }

        private string GetLocaleKey(Entity prefabEntity, PaletteFilterTypeData.PaletteFilterType paletteFilterType)
        {
            if (m_PrefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefabBase))
            {
                switch (paletteFilterType)
                {
                    case PaletteFilterTypeData.PaletteFilterType.None:
                        return string.Empty;
                    case PaletteFilterTypeData.PaletteFilterType.Theme:
                        return $"Assets.THEME[{prefabBase.name}]";
                    case PaletteFilterTypeData.PaletteFilterType.Pack:
                        return $"Assets.NAME[{prefabBase.name}]";
                    case PaletteFilterTypeData.PaletteFilterType.ZoningType:
                        return $"Assets.NAME[{prefabBase.name}]";
                    default:
                        return string.Empty;
                }
            }

            return string.Empty;
        }

        private void SetFilterChoice(int index, Entity prefabEntity)
        {
            if (m_SelectedFilterPrefabEntities.Value.Length > index)
            {
                m_SelectedFilterPrefabEntities.Value[index] = prefabEntity;
                m_SelectedFilterPrefabEntities.Binding.TriggerUpdate();
            }
        }

        private void AddFilterChoice()
        {
            if (m_SelectedFilterPrefabEntities.Value.Length == 0 ||
                m_FilterEntities.Value.Length <= m_SelectedFilterPrefabEntities.Value.Length)
            {
                return;
            }

            List<Entity> prefabEntities = m_SelectedFilterPrefabEntities.Value.ToList();
            for (int i = 0; i < m_FilterEntities.Value.Length; i++)
            {
                if (!prefabEntities.Contains(m_FilterEntities.Value[i].FilterPrefabEntity))
                {
                    prefabEntities.Add(m_FilterEntities.Value[i].FilterPrefabEntity);
                    break;
                }
            }

            m_SelectedFilterPrefabEntities.Value = prefabEntities.ToArray();
            m_SelectedFilterPrefabEntities.Binding.TriggerUpdate();
        }

        private void RemoveFilterChoice(int index)
        {
            if (m_SelectedFilterPrefabEntities.Value.Length > index && index >= 0)
            {
                Entity[] prefabEntities = new Entity[m_SelectedFilterPrefabEntities.Value.Length - 1];
                int j = 0;
                for (int i = 0; i < m_SelectedFilterPrefabEntities.Value.Length; i++)
                {
                    if (i != index)
                    {
                        prefabEntities[j++] = m_SelectedFilterPrefabEntities.Value[i];
                    }
                }

                m_SelectedFilterPrefabEntities.Value = prefabEntities;
                m_SelectedFilterPrefabEntities.Binding.TriggerUpdate();
            }
        }

        private string[] GetFilterNames()
        {
            if (m_SelectedFilterPrefabEntities.Value.Length == 0 ||
                m_PaletteFilterType.Value == PaletteFilterTypeData.PaletteFilterType.None)
            {
                return new string[0];
            }
            else
            {
                List<string> filterPrefabNames = new List<string>();
                for (int i = 0; i < m_SelectedFilterPrefabEntities.Value.Length; i++)
                {
                    if (m_PrefabSystem.TryGetPrefab(m_SelectedFilterPrefabEntities.Value[i], out PrefabBase prefabBase))
                    {
                        filterPrefabNames.Add(prefabBase.name);
                    }
                }

                return filterPrefabNames.ToArray();
            }
        }

        private bool CurrentListContainsPrefabEntity(Entity prefabEntity)
        {
            for (int i = 0; i < m_FilterEntities.Value.Length; i++)
            {
                if (m_FilterEntities.Value[i].FilterPrefabEntity == prefabEntity && prefabEntity != Entity.Null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
