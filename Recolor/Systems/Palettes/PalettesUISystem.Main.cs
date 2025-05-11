// <copyright file="PalettesUISystem.Main.cs" company="Yenyang's Mods. MIT License">
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
    using Game.Modding.Toolchain;
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
    using Unity.Entities.Serialization;
    using UnityEngine;

    /// <summary>
    /// A UI System for Palettes and Swatches.
    /// </summary>
    public partial class PalettesUISystem : ExtendedUISystemBase
    {
        private const string GeneratedPaletteNamePrefix = "Custom Palette ";
        private const string GeneratedSubcategoryNamePrefix = "Subcategory ";
        private readonly Color DefaultNoneColor = new (45f / 255f, 47f / 255f, 55f / 255f);

        private PrefabSystem m_PrefabSystem;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;
        private PaletteInstanceManagerSystem m_PaletteInstanceManagerSystem;
        private ObjectToolSystem m_ObjectToolSystem;
        private ToolSystem m_ToolSystem;
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
        private EntityQuery m_PaletteCategoryQuery;
        private EntityQuery m_AssetPackQuery;
        private EntityQuery m_ZonePrefabEntityQuery;
        private EntityQuery m_ThemePrefabEntityQuery;
        private ValueBindingHelper<PaletteFilterTypeData.PaletteFilterType> m_PaletteFilterType;
        private ValueBindingHelper<PaletteFilterEntityUIData[]> m_FilterEntities;
        private ValueBindingHelper<Entity[]> m_SelectedFilterPrefabEntities;
        private ValueBindingHelper<string[]> m_SupportedLocaleCodes;
        private ValueBindingHelper<string> m_ActiveLocaleCode;
        private ValueBindingHelper<string> m_FallbackLocaleCode;
        private ValueBindingHelper<LocalizationUIData[][]> m_LocalizationUIDatas;
        private ValueBindingHelper<PaletteChooserUIData> m_PaletteChoicesDuringPlacementDatas;
        private ValueBindingHelper<bool> m_ShowPaletteChooserDuringPlacement;
        private ValueBindingHelper<Color[]> m_NonePaletteColors;
        private ValueBindingHelper<bool> m_MinimizePaletteChooserDuringPlacement;

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
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_ObjectToolSystem = World.GetOrCreateSystemManaged<ObjectToolSystem>();

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
            m_PaletteFilterType = CreateBinding("SelectedFilterType", PaletteFilterTypeData.PaletteFilterType.None);
            m_FilterEntities = CreateBinding("FilterEntities", new PaletteFilterEntityUIData[0]);
            m_SelectedFilterPrefabEntities = CreateBinding("SelectedFilterPrefabEntities", new Entity[0]);
            m_SupportedLocaleCodes = CreateBinding("SupportedLocaleCodes", GameManager.instance.localizationManager.GetSupportedLocales());
            m_ActiveLocaleCode = CreateBinding("ActiveLocale", GameManager.instance.localizationManager.activeLocaleId);
            m_FallbackLocaleCode = CreateBinding("FallbackLocale", GameManager.instance.localizationManager.fallbackLocaleId);
            m_LocalizationUIDatas = CreateBinding("LocalizationDatas", new LocalizationUIData[2][] { new LocalizationUIData[] { new LocalizationUIData(GameManager.instance.localizationManager.activeLocaleId, string.Empty, string.Empty), }, new LocalizationUIData[] { new LocalizationUIData(GameManager.instance.localizationManager.activeLocaleId, string.Empty, string.Empty), } });
            m_PaletteChoicesDuringPlacementDatas = CreateBinding("PaletteChoicesDuringPlacement", new PaletteChooserUIData());
            m_ShowPaletteChooserDuringPlacement = CreateBinding("ShowPaletteChooserDuringPlacement", Mod.Instance.Settings.ShowPalettesOptionDuringPlacement);
            m_NonePaletteColors = CreateBinding("NonePaletteColors", new Color[3] { DefaultNoneColor, DefaultNoneColor, DefaultNoneColor });
            m_MinimizePaletteChooserDuringPlacement = CreateBinding("MinimizeDuringPlacement", Mod.Instance.Settings.MinimizePaletteChooserDuringPlacement || Mod.Instance.Settings.AlwaysMinimizedAtGameStart);

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
            CreateTrigger("DeleteSubcategory", DeleteSubcategory);
            CreateTrigger<int>("SetFilter", SetFilter);
            CreateTrigger<int, Entity>("SetFilterChoice", SetFilterChoice);
            CreateTrigger("AddFilterChoice", AddFilterChoice);
            CreateTrigger<int>("RemoveFilterChoice", RemoveFilterChoice);
            CreateTrigger<string, int>("ChangeLocaleCode", ChangeLocaleCode);
            CreateTrigger("AddLocale", AddLocale);
            CreateTrigger<string>("RemoveLocale", RemoveLocale);
            CreateTrigger<int, int, string>("ChangeLocalizedName", ChangeLocalizedName);
            CreateTrigger<int, int, string>("ChangeLocalizedDescription", ChangeLocalizedDescription);
            CreateTrigger<int, Entity>("AssignPaletteDuringPlacement", AssignPaletteDuringPlacementAction);
            CreateTrigger<int>("RemovePaletteDuringPlacement", RemovePaletteDuringPlacementAction);
            CreateTrigger("MinimizePalettesDuringPlacement", MinimizePalettesDuringPlacement);
            CreateTrigger("MaximizePalettesDuringPlacement", MaximizePalettesDuringPlacement);
            CreateTrigger("HidePalettesDuringPlacement", HidePalettesDuringPlacement);

            m_SubcategoryQuery = SystemAPI.QueryBuilder()
                .WithAll<PaletteSubcategoryData>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_PaletteCategoryQuery = SystemAPI.QueryBuilder()
                .WithAll<PaletteCategoryData>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_AssetPackQuery = SystemAPI.QueryBuilder()
                .WithAll<AssetPackData>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_ThemePrefabEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<ThemeData>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_ZonePrefabEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<ZoneData, UIObjectData>()
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

                InitializeLocalizationUIDatas();
            }
        }


        private void HandleCategoryClick(int category, int menu)
        {
            if (menu < 0 || menu >= m_PaletteCategories.Value.Length)
            {
                return;
            }

            if (menu == (int)MenuType.Palette)
            {
                m_SelectedSubcategory.Value = SIPColorFieldsSystem.NoSubcategoryName;
            }

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

        private string GenerateUniqueName(string prefabType, string prefix)
        {
            int i = 1;
            while (m_PrefabSystem.TryGetPrefab(new PrefabID(prefabType, prefix + i), out _))
            {
                i++;
            }

            return prefix + i;
        }
    }
}
