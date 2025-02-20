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
    /// A custom prefab for color swatches.
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
        [CanBeNull]
        public PrefabBase m_SubCategoryPrefab;

        /// <summary>
        /// Array of swatch info that stores swatches.
        /// </summary>
        [NotNull]
        public SwatchInfo[] m_Swatches;

        /// <summary>
        /// Array of palette filters for controlling visibility.
        /// </summary>
        [CanBeNull]
        public PaletteFilterInfo[] m_PaletteFilter;

        /// <inheritdoc/>
        public override void GetPrefabComponents(HashSet<ComponentType> components)
        {
            base.GetPrefabComponents(components);
            components.Add(ComponentType.ReadWrite<Swatch>());
            components.Add(ComponentType.ReadWrite<PaletteCategoryData>());
            if (m_PaletteFilter != null)
            {
                components.Add(ComponentType.ReadWrite<PaletteFilterData>());
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

            DynamicBuffer<Swatch> buffer = entityManager.GetBuffer<Swatch>(entity);
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

            if (m_PaletteFilter != null)
            {
                DynamicBuffer<PaletteFilterData> paleteFilterDatas = entityManager.GetBuffer<PaletteFilterData>(entity);
                paleteFilterDatas.Clear();

                for (int i = 0; i < m_PaletteFilter.Length; i++)
                {
                    if (m_PaletteFilter[i].m_FilterPrefab != null &&
                        prefabSystem.TryGetEntity(m_PaletteFilter[i].m_FilterPrefab, out Entity prefabEntity))
                    {
                        paleteFilterDatas.Add(new PaletteFilterData(prefabEntity, m_PaletteFilter[i].m_FilterType));
                    }
                }
            }

            PaletteCategoryData paleteCategoryData = new PaletteCategoryData(m_Category);

            if (m_SubCategoryPrefab != null &&
                prefabSystem.TryGetEntity(m_SubCategoryPrefab, out Entity subCategoryPrefabEntity))
            {
                paleteCategoryData.m_SubCategory = subCategoryPrefabEntity;
            }

            entityManager.SetComponentData(entity, paleteCategoryData);
        }
    }
}
