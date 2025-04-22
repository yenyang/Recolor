// <copyright file="PalettePrefabSerializeFormat.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette.Prefabs
{
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
        public string m_SubCategoryPrefabName;

        /// <summary>
        /// Array of swatch info that stores swatches.
        /// </summary>
        public SwatchInfo[] m_Swatches;

        /// <summary>
        /// Array of palette filters for controlling visibility.
        /// </summary>
        public PaletteFilterTypeData.PaletteFilterType m_FilterType;

        public string[] m_FilterNames;

        public string m_Name;

        public int m_Version = 1;

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
            m_SubCategoryPrefabName = palettePrefab.m_SubCategoryPrefabName;
            m_Swatches = palettePrefab.m_Swatches;
            m_FilterType = palettePrefab.m_FilterType;
            m_FilterNames = palettePrefab.m_FilterNames;
            m_Name = palettePrefab.name;
        }

        /// <summary>
        /// Assigns values to palette prefab.
        /// </summary>
        /// <param name="palettePrefab">Palette prefab to add values to.</param>
        public void AssignValuesToPrefab(ref PalettePrefab palettePrefab)
        {
            palettePrefab.m_Category = m_Category;
            palettePrefab.m_FilterNames = m_FilterNames;
            palettePrefab.m_FilterType = m_FilterType;
            palettePrefab.m_SubCategoryPrefabName = m_SubCategoryPrefabName;
            palettePrefab.m_Swatches = m_Swatches;
            palettePrefab.name = m_Name;
            palettePrefab.active = true;
        }
    }
}
