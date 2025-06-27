// <copyright file="PalettePrefab.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette.Prefabs
{
    using System.Collections.Generic;
    using Colossal.Annotations;
    using Game.Prefabs;
    using Recolor.Domain.Palette;
    using Unity.Entities;

    /// <summary>
    /// A custom prefab for color palettes. Although this may look similar to vanilla prefabs, It may not follow format exactly. Serialization/Deserialization is done manually by the mod.
    /// </summary>
    public class PalettePrefab : PrefabBase
    {
        /// <summary>
        /// Top level pallete category for swatches.
        /// </summary>
        public PaletteCategoryData.PaletteCategory m_Category;

        /// <summary>
        /// Subcategory prefab for swatches.
        /// </summary>
        public string m_SubCategoryPrefabName;

        /// <summary>
        /// Array of swatch info that stores swatches.
        /// </summary>
        [NotNull]
        public SwatchInfo[] m_Swatches;

        /// <summary>
        /// Type of filter.
        /// </summary>
        public PaletteFilterTypeData.PaletteFilterType m_FilterType;

        /// <summary>
        /// Array of palette filters names for controlling visibility.
        /// </summary>
        public string[] m_FilterNames;

        /// <inheritdoc/>
        public override void GetPrefabComponents(HashSet<ComponentType> components)
        {
            base.GetPrefabComponents(components);
            components.Add(ComponentType.ReadWrite<SwatchData>());
            components.Add(ComponentType.ReadWrite<PaletteCategoryData>());
            if (m_FilterType != PaletteFilterTypeData.PaletteFilterType.None &&
                m_FilterNames.Length > 0)
            {
                components.Add(ComponentType.ReadWrite<PaletteFilterEntityData>());
                components.Add(ComponentType.ReadWrite<PaletteFilterTypeData>());
            }
        }

        /// <inheritdoc/>
        public override void Initialize(EntityManager entityManager, Entity entity)
        {
            base.Initialize(entityManager, entity);
            HashSet<ComponentType> components = new HashSet<ComponentType>();
            GetPrefabComponents(components);
            foreach (ComponentType component in components)
            {
               entityManager.AddComponent(entity, component);
            }

            DynamicBuffer<SwatchData> buffer = entityManager.GetBuffer<SwatchData>(entity);
            buffer.Clear();

            for (int i = 0; i < m_Swatches.Length; i++)
            {
                buffer.Add(new SwatchData(m_Swatches[i]));
            }
        }

        /// <inheritdoc/>
        public override void LateInitialize(EntityManager entityManager, Entity entity)
        {
            base.LateInitialize(entityManager, entity);
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();

            if (m_FilterType != PaletteFilterTypeData.PaletteFilterType.None &&
                m_FilterNames.Length > 0)
            {
                DynamicBuffer<PaletteFilterEntityData> paleteFilterDatas = entityManager.GetBuffer<PaletteFilterEntityData>(entity);
                paleteFilterDatas.Clear();

                string prefabTypeName;

                switch (m_FilterType)
                {
                    case PaletteFilterTypeData.PaletteFilterType.Theme:
                        prefabTypeName = nameof(ThemePrefab);
                        break;
                    case PaletteFilterTypeData.PaletteFilterType.ZoningType:
                        prefabTypeName = nameof(ZonePrefab);
                        break;
                    case PaletteFilterTypeData.PaletteFilterType.Pack:
                        prefabTypeName = nameof(AssetPackPrefab);
                        break;
                    default:
                        prefabTypeName = nameof(ThemePrefab);
                        break;
                }

                for (int i = 0; i < m_FilterNames.Length; i++)
                {
                    if (m_FilterNames[i] != string.Empty &&
                        prefabSystem.TryGetPrefab(new PrefabID(prefabTypeName, m_FilterNames[i]), out PrefabBase filterPrefabBase) &&
                      ((filterPrefabBase is ThemePrefab && m_FilterType == PaletteFilterTypeData.PaletteFilterType.Theme) ||
                       (filterPrefabBase is ZonePrefab && m_FilterType == PaletteFilterTypeData.PaletteFilterType.ZoningType) ||
                       (filterPrefabBase is AssetPackPrefab && m_FilterType == PaletteFilterTypeData.PaletteFilterType.Pack)) &&
                        prefabSystem.TryGetEntity(filterPrefabBase, out Entity filterPrefabEntity))
                    {
                        paleteFilterDatas.Add(new PaletteFilterEntityData(filterPrefabEntity));
                    }
                }

                if (paleteFilterDatas.Length == 0)
                {
                    entityManager.RemoveComponent<PaletteFilterEntityData>(entity);
                    entityManager.RemoveComponent<PaletteFilterTypeData>(entity);
                    m_FilterType = PaletteFilterTypeData.PaletteFilterType.None;
                }
                else
                {
                    entityManager.SetComponentData(entity, new PaletteFilterTypeData() { m_FilterType = m_FilterType });
                }
            }

            PaletteCategoryData paleteCategoryData = new PaletteCategoryData(m_Category);

            if (m_SubCategoryPrefabName != string.Empty &&
                prefabSystem.TryGetPrefab(new PrefabID(nameof(PaletteSubCategoryPrefab), m_SubCategoryPrefabName), out PrefabBase prefabBase) &&
                prefabBase is PaletteSubCategoryPrefab &&
                prefabSystem.TryGetEntity(prefabBase, out Entity subCategoryPrefabEntity))
            {
                paleteCategoryData.m_SubCategory = subCategoryPrefabEntity;
                PaletteSubCategoryPrefab paletteSubCategoryPrefab = prefabBase as PaletteSubCategoryPrefab;
                paleteCategoryData.m_Category = paletteSubCategoryPrefab.m_Category;
            }

            entityManager.SetComponentData(entity, paleteCategoryData);
        }
    }
}
