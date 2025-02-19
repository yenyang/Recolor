// <copyright file="PalettesUISystem.cs" company="Yenyang's Mods. MIT License">
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
        private const string NoSubcategoryName = "No Subcategory";

        private PrefabSystem m_PrefabSystem;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;
        private ValueBindingHelper<SwatchUIData[]> m_Swatches;
        private ValueBindingHelper<string> m_UniqueName;
        private ValueBindingHelper<PaletteCategoryData.PaletteCategory> m_CurrentPaletteCategory;
        private ValueBindingHelper<bool> m_ShowPaletteEditorPanel;
        private ILog m_Log;
        private string m_ContentFolder;
        private Unity.Mathematics.Random m_Random;
        private ValueBindingHelper<PaletteChooserUIData> m_PaletteChooserData;
        private EntityQuery m_PaletteQuery;

        /// <summary>
        /// Gets the mods data folder for palette prefabs.
        /// </summary>
        public string ModsDataFolder { get { return m_ContentFolder; } }

        /// <summary>
        /// Updates the palettes binding.
        /// </summary>
        public void UpdatePalettes()
        {
            m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} starting");
            NativeArray<Entity> palettePrefabEntities = m_PaletteQuery.ToEntityArray(Allocator.Temp);

            Dictionary<string, List<SwatchUIData[]>> paletteChooserBuilder = new Dictionary<string, List<SwatchUIData[]>>();
            m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} palettePrefabs.length = {palettePrefabEntities.Length}.");
            paletteChooserBuilder.Add(NoSubcategoryName, new List<SwatchUIData[]>());
            foreach (Entity palettePrefabEntity in palettePrefabEntities)
            {
                if (!EntityManager.TryGetBuffer(palettePrefabEntity, isReadOnly: true, out DynamicBuffer<SwatchData> swatches) ||
                    swatches.Length < 2)
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} skipping palette entity {palettePrefabEntity.Index}:{palettePrefabEntity.Version}.");
                    continue;
                }

                SwatchUIData[] palette = new SwatchUIData[swatches.Length];
                for (int i = 0; i < swatches.Length; i++)
                {
                    palette[i] = new SwatchUIData(swatches[i]);
                }

                if (!EntityManager.TryGetComponent(palettePrefabEntity, out PaletteSubcategoryData subcategoryData))
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} doesn't have subcategorydata.");
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} paletteChooserBuilder[NoSubcategoryName].count = {paletteChooserBuilder[NoSubcategoryName].Count}");
                    paletteChooserBuilder[NoSubcategoryName].Add(palette);

                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} adding palette entity {palettePrefabEntity.Index}:{palettePrefabEntity.Version} with {swatches.Length} swatches.");
                }
            }

            m_PaletteChooserData.Value = new PaletteChooserUIData(paletteChooserBuilder);
            m_PaletteChooserData.Binding.TriggerUpdate();
            m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} complete");
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

            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id, ".PalettePrefabs");
            System.IO.Directory.CreateDirectory(m_ContentFolder);

            // Create bindings with the UI for transfering data to the UI.
            m_Swatches = CreateBinding("Swatches", new SwatchUIData[] { new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100), new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100) });
            m_UniqueName = CreateBinding("UniqueName", string.Empty);
            m_CurrentPaletteCategory = CreateBinding("PaletteCategory", PaletteCategoryData.PaletteCategory.Any);
            m_ShowPaletteEditorPanel = CreateBinding("ShowPaletteEditorMenu", false);
            m_PaletteChooserData = CreateBinding("PaletteChooserData", new PaletteChooserUIData());

            // Listen to trigger event that are sent from the UI to the C#.
            CreateTrigger("TrySavePalette", TrySavePalette);
            CreateTrigger<string>("ChangeUniqueName", (name) => m_UniqueName.Value = name);
            CreateTrigger("TogglePaletteEditorMenu", () => m_ShowPaletteEditorPanel.Value = !m_ShowPaletteEditorPanel.Value);
            CreateTrigger<int>("ToggleCategory", HandleCategoryClick);
            CreateTrigger<int>("RemoveSwatch", RemoveSwatch);
            CreateTrigger("PasteSwatchColor", (int swatch) => ChangeSwatchColor(swatch, m_SIPColorFieldsSystem.CopiedColor));
            CreateTrigger<int, Color>("ChangeSwatchColor", ChangeSwatchColor);
            CreateTrigger<int, int>("ChangeProbabilityWeight", ChangeProbabilityWeight);
            CreateTrigger("AddASwatch", AddASwatch);
            CreateTrigger("RandomizeSwatch", (int swatch) => ChangeSwatchColor(swatch, new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1)));
            CreateTrigger("SetSelectedPaletteIndex", (int channel, int index) =>
            {
                m_PaletteChooserData.Value.SetSelectedPaletteIndex(channel, index);
                m_PaletteChooserData.Binding.TriggerUpdate();
            });
            CreateTrigger("SetSelectedSubcategoryIndex", (int channel, int subcategoryIndex) =>
            {
                m_PaletteChooserData.Value.SetSelectedSubcategoryIndex(channel, subcategoryIndex);
                m_PaletteChooserData.Binding.TriggerUpdate();
            });

            m_PaletteQuery = SystemAPI.QueryBuilder()
                  .WithAll<SwatchData>()
                  .WithNone<Deleted, Temp>()
                  .Build();

            m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)}");
            Enabled = false;
        }

        private void TrySavePalette()
        {
            try
            {
                PalettePrefab palettePrefabBase = ScriptableObject.CreateInstance<PalettePrefab>();
                palettePrefabBase.active = true;
                palettePrefabBase.name = m_UniqueName.Value;
                palettePrefabBase.m_Category = m_CurrentPaletteCategory.Value;
                palettePrefabBase.m_Swatches = GetSwatchInfos();

                // Palette Filters are not implemented yet.
                palettePrefabBase.m_PaletteFilter = null;

                // SubCategories are not implemented yet.
                palettePrefabBase.m_SubCategoryPrefab = null;

                if (m_PrefabSystem.AddPrefab(palettePrefabBase) &&
                    m_PrefabSystem.TryGetEntity(palettePrefabBase, out Entity prefabEntity))
                {
                    palettePrefabBase.Initialize(EntityManager, prefabEntity);
                    palettePrefabBase.LateInitialize(EntityManager, prefabEntity);

                    System.IO.Directory.CreateDirectory(Path.Combine(m_ContentFolder, palettePrefabBase.name));
                    File.WriteAllText(
                        Path.Combine(m_ContentFolder, palettePrefabBase.name, $"{nameof(PalettePrefab)}-{palettePrefabBase.name}.json"),
                        JsonConvert.SerializeObject(palettePrefabBase, Formatting.Indented, settings: new JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }));
                    m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Sucessfully created, initialized, and saved prefab {nameof(PalettePrefab)}:{palettePrefabBase.name}!");
                    UpdatePalettes();
                }
            }
            catch (Exception ex)
            {
                m_Log.Error($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Could not create or initialize prefab {nameof(PalettePrefab)}:{m_UniqueName.Value}. Encountered Exception: {ex}. ");
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

        private void HandleCategoryClick(int category)
        {
            PaletteCategoryData.PaletteCategory toggledPaletteCategory = (PaletteCategoryData.PaletteCategory)category;
            PaletteCategoryData.PaletteCategory currentPaletteCategory = m_CurrentPaletteCategory.Value;
            if (toggledPaletteCategory == PaletteCategoryData.PaletteCategory.Any)
            {
                m_CurrentPaletteCategory.Value = PaletteCategoryData.PaletteCategory.Any;
                return;
            }
            else if ((m_CurrentPaletteCategory.Value & toggledPaletteCategory) == toggledPaletteCategory)
            {
                currentPaletteCategory &= ~toggledPaletteCategory;
            }
            else
            {
                currentPaletteCategory |= toggledPaletteCategory;
            }

            if (currentPaletteCategory == (PaletteCategoryData.PaletteCategory.Vehicles | PaletteCategoryData.PaletteCategory.Buildings | PaletteCategoryData.PaletteCategory.Props))
            {
                m_CurrentPaletteCategory.Value = PaletteCategoryData.PaletteCategory.Any;
            }
            else
            {
                m_CurrentPaletteCategory.Value = currentPaletteCategory;
            }
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
    }
}
