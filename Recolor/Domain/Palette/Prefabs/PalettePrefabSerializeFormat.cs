// <copyright file="PalettePrefabSerializeFormat.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette.Prefabs
{
    using Game.Prefabs;

    /// <summary>
    /// Class to manually serialize palette prefab information in a custom format.
    /// </summary>
    public class PalettePrefabSerializeFormat
    {
        /// <summary>
        /// Top level pallete category for swatches.
        /// </summary>
        public PaletteCategoryData.PaletteCategory m_Category;

        /// <summary>
        /// Subcategory prefab for swatches.
        /// </summary>
        public PrefabBase m_SubCategoryPrefab;

        /// <summary>
        /// Array of swatch info that stores swatches.
        /// </summary>
        public SwatchInfo[] m_Swatches;

        /// <summary>
        /// Array of palette filters for controlling visibility.
        /// </summary>
        public PaletteFilterInfo[] m_PaletteFilter;

        public string m_name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PalettePrefabSerializeFormat"/> class.
        /// </summary>
        public PalettePrefabSerializeFormat()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PalettePrefabSerializeFormat"/> class.
        /// </summary>
        /// <param name="palettePrefab">Palette prefab.</param>
        public PalettePrefabSerializeFormat(PalettePrefab palettePrefab)
        {
            m_Category = palettePrefab.m_Category;
            m_SubCategoryPrefab = palettePrefab.m_SubCategoryPrefab;
            m_Swatches = palettePrefab.m_Swatches;
            m_PaletteFilter = palettePrefab.m_PaletteFilter;
            m_name = palettePrefab.name;
        }

        /// <summary>
        /// Assigns values to palette prefab.
        /// </summary>
        /// <param name="palettePrefab">Palette prefab to add values to.</param>
        public void AssignValuesToPrefab(ref PalettePrefab palettePrefab)
        {
            palettePrefab.m_Category = m_Category;
            palettePrefab.m_PaletteFilter = m_PaletteFilter;
            palettePrefab.m_SubCategoryPrefab = m_SubCategoryPrefab;
            palettePrefab.m_Swatches = m_Swatches;
            palettePrefab.name = m_name;
            palettePrefab.active = true;
        }
    }
}
