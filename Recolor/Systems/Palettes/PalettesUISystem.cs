// <copyright file="PalettesUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using Colossal.Logging;
    using Recolor.Domain.Palette;
    using Recolor.Domain.Palette.Prefabs;
    using Recolor.Extensions;
    using System.Collections.Generic;
    using Unity.Entities;
    using UnityEngine;
    using System;
    using Game.Prefabs;
    using Colossal.PSI.Environment;
    using System.IO;
    using Colossal.Json;
    using Newtonsoft.Json;

    /// <summary>
    /// A UI System for Palettes and Swatches.
    /// </summary>
    public partial class PalettesUISystem : ExtendedUISystemBase
    {
        private PrefabSystem m_PrefabSystem;
        private ValueBindingHelper<SwatchUIData[]> m_Swatches;
        private ValueBindingHelper<string> m_UniqueName;
        private ValueBindingHelper<PaletteCategoryData.PaletteCategory> m_PaletteCategory;
        private ILog m_Log;
        private string m_ContentFolder;

        /// <summary>
        /// Gets the mods data folder for palette prefabs.
        /// </summary>
        public string ModsDataFolder { get { return m_ContentFolder; } }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id, ".PalettePrefabs");
            System.IO.Directory.CreateDirectory(m_ContentFolder);

            // Create bindings with the UI for transfering data to the UI.
            m_Swatches = CreateBinding("Swatches", new SwatchUIData[] { new SwatchUIData(UnityEngine.Color.white, 100, 0), new SwatchUIData(UnityEngine.Color.black, 100, 1) });
            m_UniqueName = CreateBinding("UniqueName", string.Empty);
            m_PaletteCategory = CreateBinding("PaletteCategory", PaletteCategoryData.PaletteCategory.Any);

            // Listen to trigger event that are sent from the UI to the C#.
            CreateTrigger("TrySavePalette", TrySavePalette);
            CreateTrigger<string>("ChangeUniqueName", (name) => m_UniqueName.Value = name);

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
                palettePrefabBase.m_Category = m_PaletteCategory.Value;
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

                    File.WriteAllText(
                        Path.Combine(m_ContentFolder, $"{nameof(PalettePrefab)}-{palettePrefabBase.name}.json"),
                        JsonConvert.SerializeObject(palettePrefabBase, Formatting.Indented, settings: new JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }));
                    m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)} Sucessfully created, initialized, and saved prefab {nameof(PalettePrefab)}:{palettePrefabBase.name}!");
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
    }
}
