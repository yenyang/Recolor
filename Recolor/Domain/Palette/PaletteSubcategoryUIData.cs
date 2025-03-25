// <copyright file="PaletteSubcategoryUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

using Unity.Entities;

namespace Recolor.Domain.Palette
{
    /// <summary>
    /// Class for Palette of Swatches data transfer with UI.
    /// </summary>
    public class PaletteSubcategoryUIData
    {
        public string m_Subcategory;
        public PaletteUIData[] m_Palettes;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteSubcategoryUIData"/> class.
        /// </summary>
        public PaletteSubcategoryUIData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteSubcategoryUIData"/> class.
        /// </summary>
        /// <param name="subcategory">Subcategory prefab name.</param>
        /// <param name="paletteUIDatas">Array of palette ui data.</param>
        public PaletteSubcategoryUIData(string subcategory, PaletteUIData[] paletteUIDatas)
        {
            m_Subcategory = subcategory;
            m_Palettes = paletteUIDatas;
        }

        /// <summary>
        /// Gets or sets the subcategory name.
        /// </summary>
        public string Subcategory
        {
            get { return m_Subcategory; }
            set { m_Subcategory = value; }
        }

        /// <summary>
        /// Gets or sets the palettes.
        /// </summary>
        public PaletteUIData[] Palettes
        {
            get { return m_Palettes; }
            set { m_Palettes = value; }
        }
    }
}
