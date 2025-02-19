// <copyright file="PaletteChooserUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Recolor.Domain.Palette
{
    using System.Collections.Generic;
    using Unity.Mathematics;

    /// <summary>
    /// A class for handing dropdown items, subcategories, and palettes for palette chooser.
    /// </summary>
    public class PaletteChooserUIData
    {
        public PaletteSubcategoryUIData[][] m_DropdownItems;
        public int[] m_SelectedIndexes;
        public int[] m_SelectedSubcategories;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteChooserUIData"/> class.
        /// </summary>
        public PaletteChooserUIData()
        {
            m_DropdownItems = new PaletteSubcategoryUIData[3][];
            m_SelectedIndexes = new int[3];
            m_SelectedSubcategories = new int[3];
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

            m_SelectedIndexes = new int[] { -1, -1, -1 };
            m_SelectedSubcategories = new int[3];
        }

        /// <summary>
        /// Gets or sets the dropdown items.
        /// </summary>
        public PaletteSubcategoryUIData[][] DropdownItems
        {
            get { return m_DropdownItems; }
            set { m_DropdownItems = value; }
        }

        /// <summary>
        /// Gets or sets the selectedIndexes.
        /// </summary>
        public int[] SelectedIndexes
        {
            get { return m_SelectedIndexes; }
            set { m_SelectedIndexes = value; }
        }

        /// <summary>
        /// Gets or sets the selectedSubcategories.
        /// </summary>
        public int[] SelectedSubcategories
        {
            get { return m_SelectedSubcategories; }
            set { m_SelectedSubcategories = value; }
        }

        /// <summary>
        /// Sets selected palette index for channel.
        /// </summary>
        /// <param name="channel">Channel 0-2.</param>
        /// <param name="index">Index selected.</param>
        public void SetSelectedPaletteIndex(int channel, int index)
        {
            if (channel >= 0 && channel <= 2)
            {
                m_SelectedIndexes[channel] = index;
            }
        }

        /// <summary>
        /// Sets selected subcategory index for channel.
        /// </summary>
        /// <param name="channel">Channel 0-2.</param>
        /// <param name="subcategoryIndex">Subcategory index selected.</param>
        public void SetSelectedSubcategoryIndex(int channel, int subcategoryIndex)
        {
            if (channel >= 0 && channel <= 2)
            {
                m_SelectedSubcategories[channel] = subcategoryIndex;
            }
        }
}
