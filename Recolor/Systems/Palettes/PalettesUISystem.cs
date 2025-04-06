// <copyright file="PalettesUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Colossal.Entities;
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Rendering;
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
        private const string GeneratedPaletteNamePrefix = "Custom Palette ";
        private const string GeneratedSubcategoryNamePrefix = "Subcategory ";

        private PrefabSystem m_PrefabSystem;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;
        private PaletteInstanceManagerSystem m_PaletteInstanceManagerSystem;
        private ValueBindingHelper<SwatchUIData[]> m_Swatches;
        private ValueBindingHelper<string[]> m_UniqueNames;
        private ValueBindingHelper<PaletteCategoryData.PaletteCategory[]> m_PaletteCategories;
        private ValueBindingHelper<string[]> m_Subcategories;
        private ValueBindingHelper<bool> m_ShowPaletteEditorPanel;
        private ValueBindingHelper<bool> m_ShowSubcategoryEditorPanel;
        private ILog m_Log;
        private string m_PalettePrefabsFolder;
        private string m_SubcategoryPrefabsFolder;
        private Unity.Mathematics.Random m_Random;
        private ValueBindingHelper<Entity> m_EditingPrefabEntity;
        private EntityQuery m_SubcategoryQuery;
        private ValueBindingHelper<string> m_SelectedSubcategory;

        /// <summary>
        /// Enum for handing common events for different menus.
        /// </summary>
        public enum MenuType
        {
            /// <summary>
            /// For Palette Editor Menu
            /// </summary>
            Palette = 0,

            /// <summary>
            /// For Subcategory editor menu.
            /// </summary>
            Subcategory = 1,
        }

        /// <summary>
        /// Gets the mods data folder for palette prefabs.
        /// </summary>
        public string PalettePrefabsModsDataFolder
        {
            get
            {
                return m_PalettePrefabsFolder;
            }
        }

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

        /// <summary>
        /// Sets up bindings so that a palette prefab can be edited with panel.
        /// </summary>
        /// <param name="prefabEntity">Palette Prefab entity.</param>
        public void EditPalette(Entity prefabEntity)
        {
            if (EntityManager.TryGetBuffer(prefabEntity, isReadOnly: true, out DynamicBuffer<SwatchData> swatchDatas) &&
                swatchDatas.Length >= 2 &&
                m_PrefabSystem.TryGetPrefab(prefabEntity, out PrefabBase prefabBase) &&
                prefabBase is PalettePrefab)
            {
                PalettePrefab palettePrefab = prefabBase as PalettePrefab;
                m_ShowPaletteEditorPanel.Value = true;
                m_UniqueNames.Value[(int)MenuType.Palette] = prefabBase.name;
                m_UniqueNames.Binding.TriggerUpdate();
                m_PaletteCategories.Value[(int)MenuType.Palette] = palettePrefab.m_Category;
                m_PaletteCategories.Binding.TriggerUpdate();
                if (palettePrefab.m_SubCategoryPrefabName == string.Empty)
                {
                    m_SelectedSubcategory.Value = SIPColorFieldsSystem.NoSubcategoryName;
                }
                else
                {
                    m_SelectedSubcategory.Value = palettePrefab.m_SubCategoryPrefabName;
                }

                m_PaletteCategories.Value[(int)MenuType.Palette] = palettePrefab.m_Category;

                SwatchUIData[] swatchUIDatas = new SwatchUIData[swatchDatas.Length];
                for (int i = 0; i < swatchUIDatas.Length; i++)
                {
                    swatchUIDatas[i] = new SwatchUIData(swatchDatas[i]);
                }

                m_Swatches.Value = swatchUIDatas;
                m_Swatches.Binding.TriggerUpdate();
                m_EditingPrefabEntity.Value = prefabEntity;
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            uint randomSeed = (uint)(DateTime.Now.Month + DateTime.Now.Day + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second);
            m_Random = new (randomSeed);

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_SIPColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();
            m_PaletteInstanceManagerSystem = World.GetOrCreateSystemManaged<PaletteInstanceManagerSystem>();

            m_PalettePrefabsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id, ".PalettePrefabs");
            System.IO.Directory.CreateDirectory(m_PalettePrefabsFolder);
            m_SubcategoryPrefabsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id, ".SubcategoryPrefabs");
            System.IO.Directory.CreateDirectory(m_SubcategoryPrefabsFolder);

            // Create bindings with the UI for transfering data to the UI.
            m_Swatches = CreateBinding("Swatches", new SwatchUIData[] { new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100), new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100) });
            m_UniqueNames = CreateBinding("UniqueNames", new string[2]);
            m_PaletteCategories = CreateBinding("PaletteCategories", new PaletteCategoryData.PaletteCategory[2] { PaletteCategoryData.PaletteCategory.Any, PaletteCategoryData.PaletteCategory.Any });
            m_ShowPaletteEditorPanel = CreateBinding("ShowPaletteEditorMenu", false);
            m_EditingPrefabEntity = CreateBinding("EditingPrefabEntity", Entity.Null);
            m_Subcategories = CreateBinding("Subcategories", new string[] { SIPColorFieldsSystem.NoSubcategoryName });
            m_SelectedSubcategory = CreateBinding("SelectedSubcategory", SIPColorFieldsSystem.NoSubcategoryName);
            m_ShowSubcategoryEditorPanel = CreateBinding("ShowSubcategoryEditorPanel", false);

            // Listen to trigger event that are sent from the UI to the C#.
            CreateTrigger("TrySavePalette", TrySavePalette);
            CreateTrigger<string, int>("ChangeUniqueName", ChangeUniqueName);
            CreateTrigger("TogglePaletteEditorMenu", () => m_ShowPaletteEditorPanel.Value = !m_ShowPaletteEditorPanel.Value);
            CreateTrigger("GenerateNewPalette", GenerateNewPalette);
            CreateTrigger<int, int>("ToggleCategory", HandleCategoryClick);
            CreateTrigger<int>("RemoveSwatch", RemoveSwatch);
            CreateTrigger("PasteSwatchColor", (int swatch) => ChangeSwatchColor(swatch, m_SIPColorFieldsSystem.CopiedColor));
            CreateTrigger<int, Color>("ChangeSwatchColor", ChangeSwatchColor);
            CreateTrigger<int, int>("ChangeProbabilityWeight", ChangeProbabilityWeight);
            CreateTrigger("AddASwatch", AddASwatch);
            CreateTrigger("RandomizeSwatch", (int swatch) => ChangeSwatchColor(swatch, new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1)));
            CreateTrigger("DeletePalette", DeletePalette);
            CreateTrigger("TrySaveSubcategory", TrySaveSubcategory);
            CreateTrigger<string>("ChangeSubcategory", ChangeSubcategory);
            CreateTrigger("ShowSubcategoryEditorPanel", () => m_ShowSubcategoryEditorPanel.Value = !m_ShowSubcategoryEditorPanel.Value);
            CreateTrigger<string>("EditSubcategory", EditSubcategory);
            CreateTrigger("GenerateNewSubcategory", GenerateNewSubcategory);

            m_SubcategoryQuery = SystemAPI.QueryBuilder()
                .WithAll<PaletteSubcategoryData>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode.IsGameOrEditor())
            {
                m_UniqueNames.Value = new string[2]
                {
                    GenerateUniquePalettePrefabName(),
                    GenerateUniqueSubcategoryPrefabName(),
                };
                m_UniqueNames.Binding.TriggerUpdate();

                UpdateSubcategories(PaletteCategoryData.PaletteCategory.Any);
            }
        }

        private void GenerateNewPalette()
        {
            m_ShowPaletteEditorPanel.Value = true;
            m_UniqueNames.Value[(int)MenuType.Palette] = GenerateUniquePalettePrefabName();
            m_UniqueNames.Binding.TriggerUpdate();
            m_PaletteCategories.Value[(int)MenuType.Palette] = PaletteCategoryData.PaletteCategory.Any;
            m_Swatches.Value = new SwatchUIData[] { new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100), new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100) };
            m_Swatches.Binding.TriggerUpdate();
            m_EditingPrefabEntity.Value = Entity.Null;
        }

        private void GenerateNewSubcategory()
        {
            m_ShowSubcategoryEditorPanel.Value = true;
            m_UniqueNames.Value[(int)MenuType.Subcategory] = GenerateUniqueSubcategoryPrefabName();
            m_UniqueNames.Binding.TriggerUpdate();
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

        private void TrySavePalette()
        {
            try
            {
                PalettePrefab palettePrefabBase;
                bool prefabEntityExists = false;

                // Existing Prefab Entity.
                if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PalettePrefab), m_UniqueNames.Value[(int)MenuType.Palette]), out PrefabBase prefabBase) &&
                    prefabBase != null &&
                    prefabBase is PalettePrefab &&
                    m_PrefabSystem.TryGetEntity(prefabBase, out Entity existingPrefabEntity) &&
                    EntityManager.TryGetBuffer(existingPrefabEntity, isReadOnly: false, out DynamicBuffer<SwatchData> swatchData))
                {
                    m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Found existing Palette Prefab Entity {nameof(PalettePrefab)}:{prefabBase.name}!");
                    prefabEntityExists = true;
                    palettePrefabBase = (PalettePrefab)prefabBase;
                }

                // New Prefab Entity
                else
                {
                    palettePrefabBase = ScriptableObject.CreateInstance<PalettePrefab>();
                    palettePrefabBase.name = m_UniqueNames.Value[(int)MenuType.Palette];
                }

                palettePrefabBase.active = true;
                palettePrefabBase.m_Category = m_PaletteCategories.Value[(int)MenuType.Palette];
                palettePrefabBase.m_Swatches = GetSwatchInfos();

                // Palette Filters are not implemented yet.
                palettePrefabBase.m_PaletteFilter = null;

                if (m_SelectedSubcategory.Value == SIPColorFieldsSystem.NoSubcategoryName ||
                   !m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PaletteSubCategoryPrefab), m_SelectedSubcategory.Value), out PrefabBase prefabBase2) ||
                    prefabBase2 is not PaletteSubCategoryPrefab)
                {
                    palettePrefabBase.m_SubCategoryPrefabName = string.Empty;
                }
                else
                {
                    palettePrefabBase.m_SubCategoryPrefabName = prefabBase2.name;
                }

                if ((prefabEntityExists ||
                     m_PrefabSystem.AddPrefab(palettePrefabBase)) &&
                     m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PalettePrefab), m_UniqueNames.Value[(int)MenuType.Palette]), out PrefabBase prefabBase1) &&
                     m_PrefabSystem.TryGetEntity(prefabBase1, out Entity prefabEntity))
                {
                    palettePrefabBase.Initialize(EntityManager, prefabEntity);
                    palettePrefabBase.LateInitialize(EntityManager, prefabEntity);

                    System.IO.Directory.CreateDirectory(Path.Combine(m_PalettePrefabsFolder, palettePrefabBase.name));
                    PalettePrefabSerializeFormat palettePrefabSerializeFormat = new PalettePrefabSerializeFormat(palettePrefabBase);

                    m_EditingPrefabEntity.Value = prefabEntity;

                    if (prefabEntityExists &&
                        m_PaletteInstanceManagerSystem.TryGetPaletteInstanceEntity(prefabEntity, out Entity paletteInstanceEntity))
                    {
                        EntityManager.AddComponent<Updated>(paletteInstanceEntity);
                        m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(TrySavePalette)} Added updated to paletteInstanceEntity {paletteInstanceEntity.Index}:{paletteInstanceEntity.Version}.");
                    }

                    File.WriteAllText(
                        Path.Combine(m_PalettePrefabsFolder, palettePrefabBase.name, $"{nameof(PalettePrefab)}-{palettePrefabBase.name}.json"),
                        JsonConvert.SerializeObject(palettePrefabSerializeFormat, Formatting.Indented, settings: new JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }));
                    m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Sucessfully created, initialized, and saved prefab {nameof(PalettePrefab)}:{palettePrefabBase.name}!");
                    m_SIPColorFieldsSystem.UpdatePalettes();
                }
            }
            catch (Exception ex)
            {
                m_Log.Error($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Could not create or initialize prefab {nameof(PalettePrefab)}:{m_UniqueNames.Value[(int)MenuType.Palette]}. Encountered Exception: {ex}. ");
            }
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
                }
            }
            catch (Exception ex)
            {
                m_Log.Error($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Could not create or initialize prefab {nameof(PaletteSubCategoryPrefab)}:{m_UniqueNames.Value[(int)MenuType.Subcategory]}. Encountered Exception: {ex}. ");
            }
        }

        private SwatchInfo[] GetSwatchInfos()
        {
            SwatchInfo[] infos = new SwatchInfo[m_Swatches.Value.Length];
            for (int i = 0; i < m_Swatches.Value.Length; i++)
            {
                infos[i] = new SwatchInfo(m_Swatches.Value[i]);
            }

            return infos;
        }

        private void HandleCategoryClick(int category, int menu)
        {
            if (menu < 0 || menu >= m_PaletteCategories.Value.Length)
            {
                return;
            }

            m_SelectedSubcategory.Value = SIPColorFieldsSystem.NoSubcategoryName;

            PaletteCategoryData.PaletteCategory toggledPaletteCategory = (PaletteCategoryData.PaletteCategory)category;
            PaletteCategoryData.PaletteCategory currentPaletteCategory = m_PaletteCategories.Value[menu];
            if (toggledPaletteCategory == PaletteCategoryData.PaletteCategory.Any)
            {
                m_PaletteCategories.Value[menu] = PaletteCategoryData.PaletteCategory.Any;
                m_PaletteCategories.Binding.TriggerUpdate();
                return;
            }
            else if ((m_PaletteCategories.Value[menu] & toggledPaletteCategory) == toggledPaletteCategory)
            {
                currentPaletteCategory &= ~toggledPaletteCategory;
            }
            else
            {
                currentPaletteCategory |= toggledPaletteCategory;
            }

            if (currentPaletteCategory == (PaletteCategoryData.PaletteCategory.Vehicles | PaletteCategoryData.PaletteCategory.Buildings | PaletteCategoryData.PaletteCategory.Props))
            {
                m_PaletteCategories.Value[menu] = PaletteCategoryData.PaletteCategory.Any;
            }
            else
            {
                m_PaletteCategories.Value[menu] = currentPaletteCategory;
            }

            m_PaletteCategories.Binding.TriggerUpdate();
        }

        private void ChangeUniqueName(string newName, int menuType)
        {
            if (menuType < 0 || menuType >= m_UniqueNames.Value.Length)
            {
                return;
            }

            m_UniqueNames.Value[menuType] = newName;
            m_UniqueNames.Binding.TriggerUpdate();
            string prefabType;

            switch (menuType)
            {
                case 1: prefabType = nameof(PaletteSubCategoryPrefab);
                    break;
                default: prefabType = nameof(PalettePrefab);
                    break;
            }

            if (m_PrefabSystem.TryGetPrefab(new PrefabID(prefabType, m_UniqueNames.Value[menuType]), out PrefabBase prefabBase) &&
                prefabBase != null &&
                prefabBase is PalettePrefab &&
                m_PrefabSystem.TryGetEntity(prefabBase, out Entity existingPrefabEntity))
            {
                m_EditingPrefabEntity.Value = existingPrefabEntity;
            }
            else if (m_EditingPrefabEntity.Value != Entity.Null)
            {
                m_EditingPrefabEntity.Value = Entity.Null;
            }
        }

        private string GenerateUniquePalettePrefabName()
        {
            return GenerateUniqueName(nameof(PalettePrefab), GeneratedPaletteNamePrefix);
        }

        private string GenerateUniqueSubcategoryPrefabName()
        {
            return GenerateUniqueName(nameof(PaletteSubCategoryPrefab), GeneratedSubcategoryNamePrefix);
        }

        private string GenerateUniqueName(string prefabType, string prefix)
        {
            int i = 1;
            while (m_PrefabSystem.TryGetPrefab(new PrefabID(prefabType, prefix + i), out _))
            {
                i++;
            }

            return prefix + i;
        }

        private void RemoveSwatch(int swatch)
        {
            if (m_Swatches.Value.Length > swatch &&
                swatch > 0)
            {
                SwatchUIData[] swatchDatas = m_Swatches.Value;
                SwatchUIData[] newSwatchDatas = new SwatchUIData[swatchDatas.Length - 1];
                int j = 0;
                for (int i = 0; i < swatchDatas.Length; i++)
                {
                    if (i != swatch)
                    {
                        newSwatchDatas[j++] = swatchDatas[i];
                    }
                }

                m_Swatches.Value = newSwatchDatas;
                m_Swatches.Binding.TriggerUpdate();
            }
        }

        private void ChangeSwatchColor(int swatch, Color color)
        {
            if (m_Swatches.Value.Length > swatch &&
                swatch >= 0)
            {
                m_Swatches.Value[swatch].SwatchColor = color;
                m_Swatches.Binding.TriggerUpdate();
            }
        }

        private void ChangeProbabilityWeight(int swatch, int weight)
        {
            if (m_Swatches.Value.Length > swatch &&
                swatch >= 0)
            {
                m_Swatches.Value[swatch].ProbabilityWeight = weight;
                m_Swatches.Binding.TriggerUpdate();
            }
        }

        private void AddASwatch()
        {
            SwatchUIData[] swatchUIDatas = m_Swatches.Value;
            SwatchUIData[] newSwatchUIDatas = new SwatchUIData[swatchUIDatas.Length + 1];

            swatchUIDatas.CopyTo(newSwatchUIDatas, 0);
            newSwatchUIDatas[swatchUIDatas.Length] = new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100);
            m_Swatches.Value = newSwatchUIDatas;
            m_Swatches.Binding.TriggerUpdate();
        }

        private void DeletePalette()
        {
            if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PalettePrefab), m_UniqueNames.Value[(int)MenuType.Palette]), out PrefabBase prefabBase) &&
                    prefabBase != null &&
                    prefabBase is PalettePrefab &&
                    m_PrefabSystem.TryGetEntity(prefabBase, out Entity existingPrefabEntity) &&
                    EntityManager.TryGetBuffer(existingPrefabEntity, isReadOnly: false, out DynamicBuffer<SwatchData> swatchData))
            {
                if (m_PaletteInstanceManagerSystem.TryGetPaletteInstanceEntity(existingPrefabEntity, out Entity paletteInstanceEntity))
                {
                    EntityManager.AddComponent<Deleted>(paletteInstanceEntity);
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(TrySavePalette)} Added deleted to paletteInstanceEntity {paletteInstanceEntity.Index}:{paletteInstanceEntity.Version}.");
                }

                m_PaletteInstanceManagerSystem.RemoveFromMap(existingPrefabEntity);
                m_PrefabSystem.RemovePrefab(prefabBase);
                try
                {
                    File.Delete(Path.Combine(m_PalettePrefabsFolder, prefabBase.name, $"{nameof(PalettePrefab)}-{prefabBase.name}.json"));
                }
                catch (Exception e)
                {
                    m_Log.Info($"Could not remove files for {prefabBase.name} encountered exception {e}.");
                }

                try
                {
                    Directory.Delete(Path.Combine(m_PalettePrefabsFolder, prefabBase.name));
                }
                catch (Exception e)
                {
                    m_Log.Info($"Could not remove directory for {prefabBase.name} encountered exception {e}.");
                }
            }

            m_ShowPaletteEditorPanel.Value = false;
            GenerateNewPalette();
        }
    }
}
