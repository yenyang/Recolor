// <copyright file="PaletteChooserUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Recolor.Domain.Palette
{
    using System.Collections.Generic;

    /// <summary>
    /// A class for handing dropdown items, subcategories, and palettes for palette chooser.
    /// </summary>
    public class PaletteChooserUIData
    {
        public PaletteSubcategoryUIData[][] m_DropdownItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteChooserUIData"/> class.
        /// </summary>
        public PaletteChooserUIData()
        {
            m_DropdownItems = new PaletteSubcategoryUIData[3][];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteChooserUIData"/> class.
        /// </summary>
        /// <param name="keyValuePairs">Dictionary of subcategorys and palettes.</param>
        public PaletteChooserUIData(Dictionary<string, List<SwatchUIData[]>> keyValuePairs)
        {
            m_DropdownItems = new PaletteSubcategoryUIData[3][];
            for (int i = 0; i < 3; i++)
            {
                m_DropdownItems[i] = new PaletteSubcategoryUIData[keyValuePairs.Count];
                int j = 0;
                foreach (KeyValuePair<string, List<SwatchUIData[]>> keyValuePair in keyValuePairs)
                {
                    m_DropdownItems[i][j++] = new PaletteSubcategoryUIData(keyValuePair.Key, keyValuePair.Value.ToArray());
                }
            }
        }

        /// <summary>
        /// Gets or sets the dropdown items.
        /// </summary>
        public PaletteSubcategoryUIData[][] DropdownItems
        {
            get { return m_DropdownItems; }
            set { m_DropdownItems = value; }
        }
    }
}
