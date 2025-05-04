// <copyright file="PalettesUISystem.Subcategories.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Colossal.Entities;
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
        /// <summary>
        /// Gets the mods data folder for Subcategory prefabs.
        /// </summary>
        public string SubcategoryPrefabsFolder
        {
            get
            {
                return m_SubcategoryPrefabsFolder;
            }
        }

        private void GenerateNewSubcategory()
        {
            m_ShowSubcategoryEditorPanel.Value = true;
            m_UniqueNames.Value[(int)MenuType.Subcategory] = GenerateUniqueSubcategoryPrefabName();
            m_UniqueNames.Binding.TriggerUpdate();
            m_PaletteCategories.Value[(int)MenuType.Subcategory] = PaletteCategoryData.PaletteCategory.Any;
            m_PaletteCategories.Binding.TriggerUpdate();
            ResetToDefaultLocalizationUIDatas(MenuType.Subcategory);
        }

        private void ChangeSubcategory(string subcategory)
        {
            m_SelectedSubcategory.Value = subcategory;
            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PaletteSubCategoryPrefab), subcategory), out PrefabBase prefabBase) &&
                prefabBase is PaletteSubCategoryPrefab)
            {
                PaletteSubCategoryPrefab paletteSubCategoryPrefab = prefabBase as PaletteSubCategoryPrefab;
                m_PaletteCategories.Value[(int)MenuType.Palette] = paletteSubCategoryPrefab.m_Category;
                m_PaletteCategories.Binding.TriggerUpdate();
            }
        }

        private void EditSubcategory(string subcategory)
        {
            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PaletteSubCategoryPrefab), subcategory), out PrefabBase prefabBase) &&
                prefabBase is PaletteSubCategoryPrefab)
            {
                PaletteSubCategoryPrefab paletteSubCategoryPrefab = prefabBase as PaletteSubCategoryPrefab;
                m_PaletteCategories.Value[(int)MenuType.Subcategory] = paletteSubCategoryPrefab.m_Category;
                m_PaletteCategories.Binding.TriggerUpdate();
                m_UniqueNames.Value[(int)MenuType.Subcategory] = subcategory;
                m_UniqueNames.Binding.TriggerUpdate();
                EditLocalizationFiles(Path.Combine(m_SubcategoryPrefabsFolder, paletteSubCategoryPrefab.name), MenuType.Subcategory, paletteSubCategoryPrefab.name);
            }
        }

        private void UpdateSubcategories(PaletteCategoryData.PaletteCategory category)
        {
            List<string> subcategories = new List<string>() { SIPColorFieldsSystem.NoSubcategoryName };
            NativeArray<Entity> subcategoryPrefabEntities = m_SubcategoryQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < subcategoryPrefabEntities.Length; i++)
            {
                if (m_PrefabSystem.TryGetPrefab(subcategoryPrefabEntities[i], out PrefabBase prefabBase) &&
                    prefabBase is PaletteSubCategoryPrefab &&
                    !subcategories.Contains(prefabBase.name))
                {
                    subcategories.Add(prefabBase.name);
                }
            }

            m_Subcategories.Value = subcategories.ToArray();
            m_Subcategories.Binding.TriggerUpdate();
        }

        private void TrySaveSubcategory()
        {
            try
            {
                PaletteSubCategoryPrefab paletteSubcategoryPrefabBase;
                bool prefabEntityExists = false;

                // Existing Prefab Entity.
                if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PaletteSubCategoryPrefab), m_UniqueNames.Value[(int)MenuType.Subcategory]), out PrefabBase prefabBase) &&
                    prefabBase != null &&
                    prefabBase is PaletteSubCategoryPrefab &&
                    m_PrefabSystem.TryGetEntity(prefabBase, out Entity existingPrefabEntity))
                {
                    m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Found existing Subcategory Prefab Entity {nameof(PaletteSubCategoryPrefab)}:{prefabBase.name}!");
                    prefabEntityExists = true;
                    paletteSubcategoryPrefabBase = (PaletteSubCategoryPrefab)prefabBase;
                }

                // New Prefab Entity
                else
                {
                    paletteSubcategoryPrefabBase = ScriptableObject.CreateInstance<PaletteSubCategoryPrefab>();
                    paletteSubcategoryPrefabBase.name = m_UniqueNames.Value[(int)MenuType.Subcategory];
                }

                paletteSubcategoryPrefabBase.active = true;
                paletteSubcategoryPrefabBase.m_Category = m_PaletteCategories.Value[(int)MenuType.Subcategory];

                if ((prefabEntityExists ||
                     m_PrefabSystem.AddPrefab(paletteSubcategoryPrefabBase)) &&
                     m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PaletteSubCategoryPrefab), m_UniqueNames.Value[(int)MenuType.Subcategory]), out PrefabBase prefabBase1) &&
                     m_PrefabSystem.TryGetEntity(prefabBase1, out Entity prefabEntity))
                {
                    paletteSubcategoryPrefabBase.Initialize(EntityManager, prefabEntity);
                    PaletteSubcategoryPrefabSerializeFormat paletteSubcategoryPrefabSerializeFormat = new PaletteSubcategoryPrefabSerializeFormat(paletteSubcategoryPrefabBase);
                    System.IO.Directory.CreateDirectory(Path.Combine(m_SubcategoryPrefabsFolder, paletteSubcategoryPrefabBase.name));

                    File.WriteAllText(
                        Path.Combine(m_SubcategoryPrefabsFolder, paletteSubcategoryPrefabBase.name, $"{nameof(PaletteSubCategoryPrefab)}-{paletteSubcategoryPrefabBase.name}.json"),
                        JsonConvert.SerializeObject(paletteSubcategoryPrefabSerializeFormat, Formatting.Indented, settings: new JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }));
                    m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Sucessfully created, initialized, and saved prefab {nameof(PaletteSubCategoryPrefab)}:{paletteSubcategoryPrefabBase.name}!");

                    UpdateSubcategories(m_PaletteCategories.Value[(int)MenuType.Palette]);
                    m_SelectedSubcategory.Value = paletteSubcategoryPrefabBase.name;
                    m_PaletteCategories.Value[(int)MenuType.Palette] = paletteSubcategoryPrefabBase.m_Category;
                    m_PaletteCategories.Binding.TriggerUpdate();
                    m_ShowSubcategoryEditorPanel.Value = false;

                    if (m_LocalizationUIDatas.Value.Length > (int)MenuType.Subcategory)
                    {
                        for (int i = 0; i < m_LocalizationUIDatas.Value[(int)MenuType.Subcategory].Length; i++)
                        {
                            TryExportLocalizationFile(Path.Combine(m_SubcategoryPrefabsFolder, paletteSubcategoryPrefabBase.name), m_LocalizationUIDatas.Value[(int)MenuType.Subcategory][i], MenuType.Subcategory, paletteSubcategoryPrefabBase.name);
                            AddLocalization(m_LocalizationUIDatas.Value[(int)MenuType.Subcategory][i], MenuType.Subcategory, paletteSubcategoryPrefabBase.name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_Log.Error($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Could not create or initialize prefab {nameof(PaletteSubCategoryPrefab)}:{m_UniqueNames.Value[(int)MenuType.Subcategory]}. Encountered Exception: {ex}. ");
            }
        }

        private string GenerateUniqueSubcategoryPrefabName()
        {
            return GenerateUniqueName(nameof(PaletteSubCategoryPrefab), GeneratedSubcategoryNamePrefix);
        }

        private void DeleteSubcategory()
        {
            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PaletteSubCategoryPrefab), m_UniqueNames.Value[(int)MenuType.Subcategory]), out PrefabBase prefabBase) &&
                prefabBase is PaletteSubCategoryPrefab &&
                m_PrefabSystem.TryGetEntity(prefabBase, out Entity prefabEntity))
            {
                NativeArray<Entity> palettePrefabEntities = m_PaletteCategoryQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < palettePrefabEntities.Length; i++)
                {
                    if (EntityManager.TryGetComponent(palettePrefabEntities[i], out PaletteCategoryData paletteCategoryData) &&
                        paletteCategoryData.m_SubCategory == prefabEntity)
                    {
                        paletteCategoryData.m_SubCategory = Entity.Null;
                        EntityManager.SetComponentData(palettePrefabEntities[i], paletteCategoryData);
                    }
                }

                m_PrefabSystem.RemovePrefab(prefabBase);

                try
                {
                    File.Delete(Path.Combine(m_SubcategoryPrefabsFolder, prefabBase.name, $"{nameof(PaletteSubCategoryPrefab)}-{prefabBase.name}.json"));
                }
                catch (Exception e)
                {
                    m_Log.Info($"Could not remove files for {prefabBase.name} encountered exception {e}.");
                }

                try
                {
                    Directory.Delete(Path.Combine(m_SubcategoryPrefabsFolder, prefabBase.name, "l10n"));
                }
                catch (Exception e)
                {
                    m_Log.Info($"Could not remove directory for {prefabBase.name} encountered exception {e}.");
                }

                try
                {
                    Directory.Delete(Path.Combine(m_SubcategoryPrefabsFolder, prefabBase.name));
                }
                catch (Exception e)
                {
                    m_Log.Info($"Could not remove directory for {prefabBase.name} encountered exception {e}.");
                }

                UpdateSubcategories(m_PaletteCategories.Value[(int)MenuType.Palette]);
                m_SIPColorFieldsSystem.UpdatePalettes();
                if (m_SelectedSubcategory == prefabBase.name)
                {
                    m_SelectedSubcategory.Value = SIPColorFieldsSystem.NoSubcategoryName;
                }
            }

            GenerateNewSubcategory();

            m_ShowSubcategoryEditorPanel.Value = false;
        }
    }
}
