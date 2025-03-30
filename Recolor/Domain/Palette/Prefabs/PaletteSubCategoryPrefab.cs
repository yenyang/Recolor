// <copyright file="PaletteSubCategoryPrefab.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette.Prefabs
{
    using System.Collections.Generic;
    using Game.Prefabs;
    using Unity.Entities;

    /// <summary>
    /// A prefab for subcategories of palettes.
    /// </summary>
    public class PaletteSubCategoryPrefab : PrefabBase
    {
        /// <summary>
        /// This subcategory is under this category.
        /// </summary>
        public PaletteCategoryData.PaletteCategory m_Category;

        /// <inheritdoc/>
        public override void GetPrefabComponents(HashSet<ComponentType> components)
        {
            base.GetPrefabComponents(components);
            components.Add(ComponentType.ReadWrite<PaletteSubcategoryData>());
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
        }
    }
}
