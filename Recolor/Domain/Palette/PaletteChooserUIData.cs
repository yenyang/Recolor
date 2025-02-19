// <copyright file="PaletteChooserUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Recolor.Domain.Palette
{
    using System.Collections.Generic;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// A class for handing dropdown items, subcategories, and palettes for palette chooser.
    /// </summary>
    public class PaletteChooserUIData
    {
        public PaletteSubcategoryUIData[][] m_DropdownItems;
        public Entity[] m_SelectedPaletteEntities;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteChooserUIData"/> class.
        /// </summary>
        public PaletteChooserUIData()
        {
            m_DropdownItems = new PaletteSubcategoryUIData[3][];
            m_SelectedPaletteEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteChooserUIData"/> class.
        /// </summary>
        /// <param name="keyValuePairs">Dictionary of subcategorys and palettes.</param>
        public PaletteChooserUIData(Dictionary<string, List<PaletteUIData>> keyValuePairs)
        {
            m_DropdownItems = new PaletteSubcategoryUIData[3][];
            for (int i = 0; i < 3; i++)
            {
                m_DropdownItems[i] = new PaletteSubcategoryUIData[keyValuePairs.Count];
                int j = 0;
                foreach (KeyValuePair<string, List<PaletteUIData>> keyValuePair in keyValuePairs)
                {
                    m_DropdownItems[i][j++] = new PaletteSubcategoryUIData(keyValuePair.Key, keyValuePair.Value.ToArray());
                }
            }

            m_SelectedPaletteEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
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
        public Entity[] SelectedPaletteEntities
        {
            get { return m_SelectedPaletteEntities ; }
            set { m_SelectedPaletteEntities = value; }
        }

        /// <summary>
        /// Sets the prefab entity for a channel.
        /// </summary>
        /// <param name="channel">Channel 0 - 2.</param>
        /// <param name="prefabEntity">Palette Prefab Entity.</param>
        public void SetPrefabEntity(int channel, Entity prefabEntity)
        {
            if (channel >= 0 && channel <= 2)
            {
                m_SelectedPaletteEntities[channel] = prefabEntity;
            }
        }
    }
}
