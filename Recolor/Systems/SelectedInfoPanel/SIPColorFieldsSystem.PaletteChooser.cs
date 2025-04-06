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
            NativeArray<Entity> palettePrefabEntities = m_PaletteQuery.ToEntityArray(Allocator.Temp);

            Dictionary<string, List<PaletteUIData>> paletteChooserBuilder = new Dictionary<string, List<PaletteUIData>>
            {
                { NoSubcategoryName, new List<PaletteUIData>() },
            };
            foreach (Entity palettePrefabEntity in palettePrefabEntities)
            {
                if (!EntityManager.TryGetBuffer(palettePrefabEntity, isReadOnly: true, out DynamicBuffer<SwatchData> swatches) ||
                    swatches.Length < 2)
                {
                    continue;
                }

                if (FilterForCategories(palettePrefabEntity))
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
                    paletteChooserBuilder[NoSubcategoryName].Add(new PaletteUIData(palettePrefabEntity, swatchData));
                }
                else if (m_PrefabSystem.TryGetPrefab(categoryData.m_SubCategory, out PrefabBase prefabBase) &&
                           prefabBase is PaletteSubCategoryPrefab)
                {
                    if (!paletteChooserBuilder.ContainsKey(prefabBase.name))
                    {
                        paletteChooserBuilder.Add(prefabBase.name, new List<PaletteUIData>());
                    }

                    paletteChooserBuilder[prefabBase.name].Add(new PaletteUIData(palettePrefabEntity, swatchData));
                }
            }

            if (EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<AssignedPalette> assignedPalettes) &&
                assignedPalettes.Length > 0)
            {
                m_PaletteChooserData.Value = new PaletteChooserUIData(paletteChooserBuilder, assignedPalettes);
            }
            else
            {
                m_PaletteChooserData.Value = new PaletteChooserUIData(paletteChooserBuilder);
            }

            m_PaletteChooserData.Binding.TriggerUpdate();
        }

        private bool FilterForCategories(Entity palettePrefabEntity)
        {
            if (m_PrefabSystem.TryGetPrefab(palettePrefabEntity, out PrefabBase prefabBase) &&
                  prefabBase is PalettePrefab)
            {
                PalettePrefab palettePrefab = prefabBase as PalettePrefab;
                if ((palettePrefab.m_Category == PaletteCategoryData.PaletteCategory.Vehicles &&
                    !EntityManager.HasComponent<Game.Vehicles.Vehicle>(m_CurrentEntity)) ||
                    (palettePrefab.m_Category == PaletteCategoryData.PaletteCategory.Buildings &&
                    !EntityManager.HasComponent<Game.Buildings.Building>(m_CurrentEntity)) ||
                    (palettePrefab.m_Category == PaletteCategoryData.PaletteCategory.Props &&
                   (!EntityManager.HasComponent<Game.Objects.Static>(m_CurrentEntity) ||
                    !EntityManager.HasComponent<Game.Objects.Object>(m_CurrentEntity) ||
                     EntityManager.HasComponent<Game.Buildings.Building>(m_CurrentEntity))))
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} wrong category: {palettePrefab.m_Category}. skipping palette entity {palettePrefabEntity.Index}:{palettePrefabEntity.Version}.");
                    return true;
                }

                if (palettePrefab.m_Category == (PaletteCategoryData.PaletteCategory.Vehicles | PaletteCategoryData.PaletteCategory.Buildings) &&
                   !EntityManager.HasComponent<Game.Vehicles.Vehicle>(m_CurrentEntity) &&
                   !EntityManager.HasComponent<Game.Buildings.Building>(m_CurrentEntity))
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} wrong categories: {palettePrefab.m_Category}. skipping palette entity {palettePrefabEntity.Index}:{palettePrefabEntity.Version}.");
                    return true;
                }

                if (palettePrefab.m_Category == (PaletteCategoryData.PaletteCategory.Vehicles | PaletteCategoryData.PaletteCategory.Props) &&
                   !EntityManager.HasComponent<Game.Vehicles.Vehicle>(m_CurrentEntity) &&
                  (!EntityManager.HasComponent<Game.Objects.Static>(m_CurrentEntity) ||
                   !EntityManager.HasComponent<Game.Objects.Object>(m_CurrentEntity) ||
                    EntityManager.HasComponent<Game.Buildings.Building>(m_CurrentEntity)))
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} wrong categories: {palettePrefab.m_Category}. skipping palette entity {palettePrefabEntity.Index}:{palettePrefabEntity.Version}.");
                    return true;
                }

                if (palettePrefab.m_Category == (PaletteCategoryData.PaletteCategory.Buildings | PaletteCategoryData.PaletteCategory.Props) &&
                   !EntityManager.HasComponent<Game.Buildings.Building>(m_CurrentEntity) &&
                  (!EntityManager.HasComponent<Game.Objects.Static>(m_CurrentEntity) ||
                   !EntityManager.HasComponent<Game.Objects.Object>(m_CurrentEntity) ||
                    EntityManager.HasComponent<Game.Buildings.Building>(m_CurrentEntity)))
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} wrong categories: {palettePrefab.m_Category}. skipping palette entity {palettePrefabEntity.Index}:{palettePrefabEntity.Version}.");
                    return true;
                }
            }

            return false;
        }

        private void AssignPaletteAction(int channel, Entity prefabEntity)
        {
            m_PaletteChooserData.Value.SetPrefabEntity(channel, prefabEntity);
            m_PaletteChooserData.Binding.TriggerUpdate();
            AssignPalette(channel, m_CurrentEntity, prefabEntity);
            m_PreviouslySelectedEntity = Entity.Null;
        }

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

        private void RemovePaletteAction(int channel)
        {
            RemovePalette(channel, m_CurrentEntity);
            m_PreviouslySelectedEntity = Entity.Null;
        }

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
    }
}
