// <copyright file="ColorPainterUISystem.Palettes.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Tools
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Recolor;
    using Recolor.Domain;
    using Recolor.Domain.Palette.Prefabs;
    using Recolor.Domain.Palette;
    using Recolor.Extensions;
    using Recolor.Systems.SelectedInfoPanel;
    using System.Collections.Generic;
    using System;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// A UI System for the Color Painter Tool.
    /// </summary>
    public partial class ColorPainterUISystem : ExtendedUISystemBase
    {
        /// <summary>
        /// Updates the referenced palettes binding.
        /// </summary>
        public void UpdatePalettes()
        {
            NativeArray<Entity> palettePrefabEntities = m_PaletteQuery.ToEntityArray(Allocator.Temp);
            Entity[] selectedEntities = m_PaletteChoicesPainterDatas.Value.SelectedPaletteEntities;
            Entity[] newSelectedEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };

            PaletteSubcategoryUIData noSubcategoryGroup = new PaletteSubcategoryUIData(SIPColorFieldsSystem.NoSubcategoryName, Entity.Null);
            Dictionary<PaletteSubcategoryUIData, List<Entity>> paletteChooserBuilder = new Dictionary<PaletteSubcategoryUIData, List<Entity>>
            {
                { noSubcategoryGroup, new List<Entity>() },
            };
            foreach (Entity palettePrefabEntity in palettePrefabEntities)
            {
                if (!m_PrefabSystem.TryGetPrefab(palettePrefabEntity, out PrefabBase prefabBase1) ||
                    prefabBase1 is not PalettePrefab)
                {
                    UnselectPalette(palettePrefabEntity, ref selectedEntities, ref m_PaletteChoicesPainterDatas);
                    continue;
                }

                PalettePrefab palettePrefabBase = prefabBase1 as PalettePrefab;

                if (!EntityManager.TryGetBuffer(palettePrefabEntity, isReadOnly: true, out DynamicBuffer<SwatchData> swatches) ||
                    swatches.Length < 2)
                {
                    UnselectPalette(palettePrefabEntity, ref selectedEntities, ref m_PaletteChoicesPainterDatas);
                    continue;
                }

                if (FilterForCategories(palettePrefabEntity))
                {
                    UnselectPalette(palettePrefabEntity, ref selectedEntities, ref m_PaletteChoicesPainterDatas);
                    continue;
                }

                if (FilterByType(palettePrefabEntity, m_PaletteFilterType.Value))
                {
                    UnselectPalette(palettePrefabEntity, ref selectedEntities, ref m_PaletteChoicesPainterDatas);
                    continue;
                }

                if (!EntityManager.TryGetComponent(palettePrefabEntity, out PaletteCategoryData categoryData) ||
                    categoryData.m_SubCategory == Entity.Null)
                {
                    paletteChooserBuilder[noSubcategoryGroup].Add(palettePrefabEntity);
                }
                else if (m_PrefabSystem.TryGetPrefab(categoryData.m_SubCategory, out PrefabBase prefabBase) &&
                           prefabBase is PaletteSubCategoryPrefab)
                {
                    bool foundSubcategory = false;
                    foreach (KeyValuePair<PaletteSubcategoryUIData, List<Entity>> keyValuePair in paletteChooserBuilder)
                    {
                        if (keyValuePair.Key.m_PrefabEntity == categoryData.m_SubCategory)
                        {
                            paletteChooserBuilder[keyValuePair.Key].Add(palettePrefabEntity);
                            foundSubcategory = true;
                            break;
                        }
                    }

                    if (!foundSubcategory)
                    {
                        PaletteSubcategoryUIData subcategoryGroup = new PaletteSubcategoryUIData(prefabBase.name, categoryData.m_SubCategory);
                        if (!paletteChooserBuilder.ContainsKey(subcategoryGroup))
                        {
                            paletteChooserBuilder.Add(subcategoryGroup, new List<Entity>());
                        }

                        paletteChooserBuilder[subcategoryGroup].Add(palettePrefabEntity);
                    }
                }

                for (int i = 0; i < Math.Min(selectedEntities.Length, 3); i++)
                {
                    if (selectedEntities[i] == palettePrefabEntity)
                    {
                        newSelectedEntities[i] = palettePrefabEntity;
                    }
                }
            }

            PaletteChooserUIData paletteChooserUIData = new PaletteChooserUIData(paletteChooserBuilder);
            paletteChooserUIData.SelectedPaletteEntities = newSelectedEntities;
            m_PaletteChoicesPainterDatas.Value = paletteChooserUIData;

            m_PaletteChoicesPainterDatas.Binding.TriggerUpdate();
        }

        /// <summary>
        /// Filters for categories such as building, vehicle, or prop.
        /// </summary>
        /// <param name="palettePrefabEntity">Prefab Entity for the palette.</param>
        /// <returns>True if does not match category of current entity. False if matches category of current entity.</returns>
        private bool FilterForCategories(Entity palettePrefabEntity)
        {
            if (m_PrefabSystem.TryGetPrefab(palettePrefabEntity, out PrefabBase prefabBase) &&
                  prefabBase is PalettePrefab)
            {
                PalettePrefab palettePrefab = prefabBase as PalettePrefab;
                PaletteCategoryData.PaletteCategory category = palettePrefab.m_Category;
                if (EntityManager.TryGetComponent(palettePrefabEntity, out PaletteCategoryData paletteCategoryData) &&
                    EntityManager.TryGetComponent(paletteCategoryData.m_SubCategory, out PaletteSubcategoryData paletteSubcategoryData))
                {
                    category = paletteSubcategoryData.m_Category;
                }

                if (category == PaletteCategoryData.PaletteCategory.Any)
                {
                    return false;
                }

                // There is currently no compatibile color painter filter type for net lane fences.
                if (((category & PaletteCategoryData.PaletteCategory.Vehicles) == PaletteCategoryData.PaletteCategory.Vehicles &&
                      ColorPainterFilterType == FilterType.Vehicles) ||
                    ((category & PaletteCategoryData.PaletteCategory.Buildings) == PaletteCategoryData.PaletteCategory.Buildings &&
                      ColorPainterFilterType == FilterType.Building) ||
                    ((category & PaletteCategoryData.PaletteCategory.Props) == PaletteCategoryData.PaletteCategory.Props &&
                      ColorPainterFilterType == FilterType.Props) ||
                    ((category & PaletteCategoryData.PaletteCategory.NetLanes) == PaletteCategoryData.PaletteCategory.NetLanes &&
                      ColorPainterFilterType == FilterType.NetLanes))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Filter by different types of filters.
        /// </summary>
        /// <param name="palettePrefabEntity">Prefab entity for the palette.</param>
        /// <param name="paletteFilterType">type of filter to check for.</param>
        /// <returns>True if current entity does not match filter type. false if matches filter type.</returns>
        private bool FilterByType(Entity palettePrefabEntity, PaletteFilterTypeData.PaletteFilterType paletteFilterType)
        {
            if (!EntityManager.HasComponent<PaletteFilterTypeData>(palettePrefabEntity))
            {
                return false;
            }

            if (paletteFilterType == PaletteFilterTypeData.PaletteFilterType.None &&
                EntityManager.HasComponent<PaletteFilterTypeData>(palettePrefabEntity))
            {
                return true;
            }

            if (!EntityManager.TryGetBuffer(palettePrefabEntity, isReadOnly: true, out DynamicBuffer<PaletteFilterEntityData> paletteFilterEntityDatas))
            {
                return true;
            }

            for (int i = 0; i < paletteFilterEntityDatas.Length; i++)
            {
                if (paletteFilterEntityDatas[i].m_PrefabEntity == m_SelectedFilterPrefabEntity.Value)
                {
                    return false;
                }
            }

            return true;
        }


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
                    m_SelectedFilterPrefabEntity.Value = Entity.Null;
                    UpdatePalettes();
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
                    m_SelectedFilterPrefabEntity.Value = Entity.Null;
                    UpdatePalettes();
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
                SetFilterChoice(paletteFilterEntityUIDatas[0].FilterPrefabEntity);
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

        private void SetFilterChoice(Entity prefabEntity)
        {
            m_SelectedFilterPrefabEntity.Value = prefabEntity;
            UpdatePalettes();
        }

        private void UnselectPalette(Entity palettePrefabEntity, ref Entity[] selectedEntities, ref ValueBindingHelper<PaletteChooserUIData> paletteChooserUIDataBinding)
        {
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                if (selectedEntities[i] == palettePrefabEntity)
                {
                    selectedEntities[i] = Entity.Null;
                }
            }

            paletteChooserUIDataBinding.Value.SelectedPaletteEntities = selectedEntities;
        }

        private bool IsSelected(Entity palettePrefabEntity, Entity[] selectedEntities)
        {
            for (int i = 0; i < selectedEntities.Length; i++)
            {
                if (selectedEntities[i] == palettePrefabEntity)
                {
                    return true;
                }
            }

            return false;
        }

        private void AssignPalettePainterAction(int channel, Entity prefabEntity)
        {
            m_PaletteChoicesPainterDatas.Value.SetPrefabEntity(channel, prefabEntity);
            m_PaletteChoicesPainterDatas.Binding.TriggerUpdate();
        }

        private void RemovePalettePainterAction(int channel)
        {
            m_PaletteChoicesPainterDatas.Value.SetPrefabEntity(channel, Entity.Null);
            m_PaletteChoicesPainterDatas.Binding.TriggerUpdate();
        }


    }
}
