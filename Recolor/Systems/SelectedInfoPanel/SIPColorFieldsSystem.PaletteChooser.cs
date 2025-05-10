// <copyright file="SIPColorFieldsSystem.PaletteChooser.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.SelectedInfoPanel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Common;
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Domain.Palette.Prefabs;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.Palettes;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    /// Partial class for Selected Info panel mostly related to Palette choosing.
    /// </summary>
    public partial class SIPColorFieldsSystem : ExtendedInfoSectionBase
    {
        /// <summary>
        /// Updates the palettes binding.
        /// </summary>
        public void UpdatePalettes()
        {
            UpdatePalettes(m_CurrentPrefabEntity, ref m_PaletteChooserData);
        }

        /// <summary>
        /// Updates the referenced palettes binding.
        /// </summary>
        /// <param name="prefabEntity">prefab entity to filter for categories.</param>
        /// <param name="paletteChooserBinding">Binding to update.</param>
        public void UpdatePalettes(Entity prefabEntity, ref ValueBindingHelper<PaletteChooserUIData> paletteChooserBinding)
        {
            NativeArray<Entity> palettePrefabEntities = m_PaletteQuery.ToEntityArray(Allocator.Temp);
            Entity[] selectedEntities = paletteChooserBinding.Value.SelectedPaletteEntities;
            Entity[] newSelectedEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
            Dictionary<string, List<PaletteUIData>> paletteChooserBuilder = new Dictionary<string, List<PaletteUIData>>
            {
                { NoSubcategoryName, new List<PaletteUIData>() },
            };
            foreach (Entity palettePrefabEntity in palettePrefabEntities)
            {
                if (!m_PrefabSystem.TryGetPrefab(palettePrefabEntity, out PrefabBase prefabBase1) ||
                    prefabBase1 is not PalettePrefab)
                {
                    continue;
                }

                PalettePrefab palettePrefabBase = prefabBase1 as PalettePrefab;

                if (!EntityManager.TryGetBuffer(palettePrefabEntity, isReadOnly: true, out DynamicBuffer<SwatchData> swatches) ||
                    swatches.Length < 2)
                {
                    continue;
                }

                if (FilterForCategories(palettePrefabEntity, prefabEntity))
                {
                    continue;
                }

                if (FilterByType(palettePrefabEntity, palettePrefabBase.m_FilterType))
                {
                    continue;
                }

                SwatchUIData[] swatchData = new SwatchUIData[swatches.Length];
                for (int i = 0; i < swatches.Length; i++)
                {
                    swatchData[i] = new SwatchUIData(swatches[i]);
                }

                if (!EntityManager.TryGetComponent(palettePrefabEntity, out PaletteCategoryData categoryData) ||
                    categoryData.m_SubCategory == Entity.Null)
                {
                    paletteChooserBuilder[NoSubcategoryName].Add(new PaletteUIData(palettePrefabEntity, swatchData, palettePrefabBase.name));
                }
                else if (m_PrefabSystem.TryGetPrefab(categoryData.m_SubCategory, out PrefabBase prefabBase) &&
                           prefabBase is PaletteSubCategoryPrefab)
                {
                    if (!paletteChooserBuilder.ContainsKey(prefabBase.name))
                    {
                        paletteChooserBuilder.Add(prefabBase.name, new List<PaletteUIData>());
                    }

                    paletteChooserBuilder[prefabBase.name].Add(new PaletteUIData(palettePrefabEntity, swatchData, palettePrefabBase.name));
                }

                for (int i = 0; i < Math.Min(selectedEntities.Length, 3); i++)
                {
                    if (selectedEntities[i] == palettePrefabEntity)
                    {
                        newSelectedEntities[i] = palettePrefabEntity;
                    }
                }
            }

            if (EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<AssignedPalette> assignedPalettes) &&
                assignedPalettes.Length > 0)
            {
                paletteChooserBinding.Value = new PaletteChooserUIData(paletteChooserBuilder, assignedPalettes);
            }
            else if (m_ToolSystem.activeTool == m_DefaultToolSystem)
            {
                paletteChooserBinding.Value = new PaletteChooserUIData(paletteChooserBuilder);
            }
            else
            {
                PaletteChooserUIData paletteChooserUIData = new PaletteChooserUIData(paletteChooserBuilder);
                paletteChooserUIData.SelectedPaletteEntities = newSelectedEntities;
            }

            paletteChooserBinding.Binding.TriggerUpdate();
        }

        /// <summary>
        /// Filters for categories such as building, vehicle, or prop.
        /// </summary>
        /// <param name="palettePrefabEntity">Prefab Entity for the palette.</param>
        /// <param name="prefabEntity">prefab entity to check against.</param>
        /// <returns>True if does not match category of current entity. False if matches category of current entity.</returns>
        public bool FilterForCategories(Entity palettePrefabEntity, Entity prefabEntity)
        {
            if (m_PrefabSystem.TryGetPrefab(palettePrefabEntity, out PrefabBase prefabBase) &&
                  prefabBase is PalettePrefab)
            {
                PalettePrefab palettePrefab = prefabBase as PalettePrefab;
                if ((palettePrefab.m_Category == PaletteCategoryData.PaletteCategory.Vehicles &&
                    !EntityManager.HasComponent<VehicleData>(prefabEntity)) ||
                    (palettePrefab.m_Category == PaletteCategoryData.PaletteCategory.Buildings &&
                    !EntityManager.HasComponent<BuildingData>(prefabEntity)) ||
                    (palettePrefab.m_Category == PaletteCategoryData.PaletteCategory.Props &&
                   (!EntityManager.HasComponent<StaticObjectData>(prefabEntity) ||
                    !EntityManager.HasComponent<ObjectData>(prefabEntity) ||
                     EntityManager.HasComponent<BuildingData>(prefabEntity))))
                {
                    return true;
                }

                if (palettePrefab.m_Category == (PaletteCategoryData.PaletteCategory.Vehicles | PaletteCategoryData.PaletteCategory.Buildings) &&
                   !EntityManager.HasComponent<VehicleData>(prefabEntity) &&
                   !EntityManager.HasComponent<BuildingData>(prefabEntity))
                {
                    return true;
                }

                if (palettePrefab.m_Category == (PaletteCategoryData.PaletteCategory.Vehicles | PaletteCategoryData.PaletteCategory.Props) &&
                   !EntityManager.HasComponent<VehicleData>(prefabEntity) &&
                  (!EntityManager.HasComponent<StaticObjectData>(prefabEntity) ||
                   !EntityManager.HasComponent<ObjectData>(prefabEntity) ||
                    EntityManager.HasComponent<BuildingData>(prefabEntity)))
                {
                    return true;
                }

                if (palettePrefab.m_Category == (PaletteCategoryData.PaletteCategory.Buildings | PaletteCategoryData.PaletteCategory.Props) &&
                   !EntityManager.HasComponent<BuildingData>(prefabEntity) &&
                  (!EntityManager.HasComponent<StaticObjectData>(prefabEntity) ||
                   !EntityManager.HasComponent<ObjectData>(prefabEntity) ||
                    EntityManager.HasComponent<BuildingData>(prefabEntity)))
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Assigns a palette to the instance entity based on prefab entity and channel.
        /// </summary>
        /// <param name="channel">Channel 0 to 2.</param>
        /// <param name="instanceEntity">The entity to add the palette to.</param>
        /// <param name="prefabEntity">Palette prefab entity.</param>
        private void AssignPalette(int channel, Entity instanceEntity, Entity prefabEntity)
        {
            if (channel < 0 || channel > 2)
            {
                return;
            }

            if (!EntityManager.HasBuffer<AssignedPalette>(instanceEntity))
            {
                EntityManager.AddBuffer<AssignedPalette>(instanceEntity);
            }

            DynamicBuffer<AssignedPalette> paletteAssignments = EntityManager.GetBuffer<AssignedPalette>(instanceEntity, isReadOnly: false);

            for (int i = 0; i < paletteAssignments.Length; i++)
            {
                if (paletteAssignments[i].m_Channel == channel)
                {
                    AssignedPalette paletteAssignment = paletteAssignments[i];
                    paletteAssignment.m_PaletteInstanceEntity = m_PaletteInstanceMangerSystem.GetOrCreatePaletteInstanceEntity(prefabEntity);
                    paletteAssignments[i] = paletteAssignment;
                    if (m_AssignedPaletteCustomColorSystem.TryGetColorFromPalette(instanceEntity, channel, out UnityEngine.Color newColor))
                    {
                        ChangeColor(channel, newColor);
                    }
                    else
                    {
                        EntityManager.AddComponent<BatchesUpdated>(instanceEntity);
                    }

                    return;
                }
            }

            AssignedPalette newPaletteAssignment = new AssignedPalette()
            {
                m_Channel = channel,
                m_PaletteInstanceEntity = m_PaletteInstanceMangerSystem.GetOrCreatePaletteInstanceEntity(prefabEntity),
            };

            paletteAssignments.Add(newPaletteAssignment);
            if (m_AssignedPaletteCustomColorSystem.TryGetColorFromPalette(instanceEntity, channel, out UnityEngine.Color color))
            {
                ChangeColor(channel, color);
            }
            else
            {
                EntityManager.AddComponent<BatchesUpdated>(instanceEntity);
            }
        }

        /// <summary>
        /// Removes a palette from a channel on an instance entity.
        /// </summary>
        /// <param name="channel">channel 0 to 2.</param>
        /// <param name="instanceEntity">instance entity to remove a palette from.</param>
        private void RemovePalette(int channel, Entity instanceEntity)
        {
            if (channel < 0 ||
                channel > 2 ||
               !EntityManager.HasBuffer<AssignedPalette>(instanceEntity))
            {
                return;
            }

            DynamicBuffer<AssignedPalette> paletteAssignments = EntityManager.GetBuffer<AssignedPalette>(instanceEntity, isReadOnly: false);

            if (paletteAssignments.Length == 1 &&
                paletteAssignments[0].m_Channel == channel)
            {
                EntityManager.RemoveComponent<AssignedPalette>(instanceEntity);
                ResetColor(channel);
            }

            for (int i = 0; i < paletteAssignments.Length; i++)
            {
                if (paletteAssignments[i].m_Channel == channel)
                {
                    paletteAssignments.RemoveAt(i);
                    ResetColor(channel);
                    return;
                }
            }
        }

        private bool FilterForCategories(Entity palettePrefabEntity)
        {
            return FilterForCategories(palettePrefabEntity, m_CurrentEntity);
        }

        private void AssignPaletteAction(int channel, Entity prefabEntity)
        {
            m_PaletteChooserData.Value.SetPrefabEntity(channel, prefabEntity);
            m_PaletteChooserData.Binding.TriggerUpdate();
            AssignPalette(channel, m_CurrentEntity, prefabEntity);
            m_PreviouslySelectedEntity = Entity.Null;
        }

        private void RemovePaletteAction(int channel)
        {
            RemovePalette(channel, m_CurrentEntity);
            m_PreviouslySelectedEntity = Entity.Null;
        }

        /// <summary>
        /// Filter by different types of filters.
        /// </summary>
        /// <param name="palettePrefabEntity">Prefab entity for the palette.</param>
        /// <param name="paletteFilterType">type of filter to check for.</param>
        /// <returns>True if current entity does not match filter type. false if matches filter type.</returns>
        private bool FilterByType(Entity palettePrefabEntity, PaletteFilterTypeData.PaletteFilterType paletteFilterType)
        {
            if (paletteFilterType == PaletteFilterTypeData.PaletteFilterType.None ||
               !EntityManager.TryGetBuffer(palettePrefabEntity, isReadOnly: true, out DynamicBuffer<PaletteFilterEntityData> paletteFilterEntityDatas))
            {
                return false;
            }

            if (paletteFilterType == PaletteFilterTypeData.PaletteFilterType.Theme &&
                EntityManager.TryGetComponent(m_CurrentPrefabEntity, out SpawnableBuildingData spawnableBuildingData) &&
                m_PrefabSystem.TryGetPrefab(spawnableBuildingData.m_ZonePrefab, out PrefabBase zonePrefabBase) &&
                zonePrefabBase is ZonePrefab)
            {
                ZonePrefab zonePrefab = zonePrefabBase as ZonePrefab;
                if (!zonePrefab.TryGet(out ThemeObject themeObject) ||
                     themeObject == null ||
                    !m_PrefabSystem.TryGetEntity(themeObject.m_Theme, out Entity themeEntity))
                {
                    return true;
                }

                for (int i = 0; i < paletteFilterEntityDatas.Length; i++)
                {
                    if (paletteFilterEntityDatas[i].m_PrefabEntity == themeEntity)
                    {
                        return false;
                    }
                }
            }
            else if (paletteFilterType == PaletteFilterTypeData.PaletteFilterType.ZoningType &&
                     EntityManager.TryGetComponent(m_CurrentPrefabEntity, out SpawnableBuildingData spawnableBuildingData2))
            {
                for (int i = 0; i < paletteFilterEntityDatas.Length; i++)
                {
                    if (paletteFilterEntityDatas[i].m_PrefabEntity == spawnableBuildingData2.m_ZonePrefab)
                    {
                        return false;
                    }
                }
            }
            else if (paletteFilterType == PaletteFilterTypeData.PaletteFilterType.Pack &&
                     EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<AssetPackElement> assetPackElements))
            {
                for (int i = 0; i < paletteFilterEntityDatas.Length; i++)
                {
                    for (int j = 0; j < assetPackElements.Length; j++)
                    {
                        if (assetPackElements[j].m_Pack == paletteFilterEntityDatas[i].m_PrefabEntity)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
