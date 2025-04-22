// <copyright file="PalettesUISystem.Palettes.cs" company="Yenyang's Mods. MIT License">
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

                if (m_PrefabSystem.TryGetPrefab(new PrefabID(nameof(PaletteSubCategoryPrefab), palettePrefab.m_SubCategoryPrefabName), out PrefabBase subcategoryPrefabBase) &&
                    subcategoryPrefabBase is PaletteSubCategoryPrefab)
                {
                    m_SelectedSubcategory.Value = palettePrefab.m_SubCategoryPrefabName;
                }
                else
                {
                    m_SelectedSubcategory.Value = SIPColorFieldsSystem.NoSubcategoryName;
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

        private void GenerateNewPalette()
        {
            m_ShowPaletteEditorPanel.Value = true;
            m_UniqueNames.Value[(int)MenuType.Palette] = GenerateUniquePalettePrefabName();
            m_UniqueNames.Binding.TriggerUpdate();
            m_PaletteCategories.Value[(int)MenuType.Palette] = PaletteCategoryData.PaletteCategory.Any;
            m_Swatches.Value = new SwatchUIData[] { new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100), new SwatchUIData(new Color(m_Random.NextFloat(), m_Random.NextFloat(), m_Random.NextFloat(), 1), 100) };
            m_Swatches.Binding.TriggerUpdate();
            m_EditingPrefabEntity.Value = Entity.Null;
            m_PaletteCategories.Value[(int)MenuType.Palette] = PaletteCategoryData.PaletteCategory.Any;
            m_PaletteCategories.Binding.TriggerUpdate();
            m_SelectedSubcategory.Value = SIPColorFieldsSystem.NoSubcategoryName;
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


        private SwatchInfo[] GetSwatchInfos()
        {
            SwatchInfo[] infos = new SwatchInfo[m_Swatches.Value.Length];
            for (int i = 0; i < m_Swatches.Value.Length; i++)
            {
                infos[i] = new SwatchInfo(m_Swatches.Value[i]);
            }

            return infos;
        }

        private string GenerateUniquePalettePrefabName()
        {
            return GenerateUniqueName(nameof(PalettePrefab), GeneratedPaletteNamePrefix);
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

                m_SIPColorFieldsSystem.UpdatePalettes();
            }

            m_ShowPaletteEditorPanel.Value = false;
            GenerateNewPalette();
        }
    }
}
