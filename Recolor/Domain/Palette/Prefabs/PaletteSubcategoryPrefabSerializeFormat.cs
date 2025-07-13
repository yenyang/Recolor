// <copyright file="PaletteSubcategoryPrefabSerializeFormat.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette.Prefabs
{
    using Game.Prefabs;

    /// <summary>
    /// Class to manually serialize palette prefab information in a custom format.
    /// </summary>
    public class PaletteSubcategoryPrefabSerializeFormat
    {
        /// <summary>
        /// Top level pallete category for swatches.
        /// </summary>
        public PaletteCategoryData.PaletteCategory m_Category;

        public string m_name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteSubcategoryPrefabSerializeFormat"/> class.
        /// </summary>
        public PaletteSubcategoryPrefabSerializeFormat()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteSubcategoryPrefabSerializeFormat"/> class.
        /// </summary>
        /// <param name="subcategoryPrefab">subcategoryPrefab.</param>
        public PaletteSubcategoryPrefabSerializeFormat(PaletteSubCategoryPrefab subcategoryPrefab)
        {
            m_Category = subcategoryPrefab.m_Category;
            m_name = subcategoryPrefab.name;
        }

        /// <summary>
        /// Assigns values to palette prefab.
        /// </summary>
        /// <param name="subcategoryPrefab">subcategoryPrefab  to add values to.</param>
        public void AssignValuesToPrefab(ref PaletteSubCategoryPrefab subcategoryPrefab)
        {
            subcategoryPrefab.m_Category = m_Category;
            subcategoryPrefab.name = m_name;
            subcategoryPrefab.active = true;
        }
    }
}
