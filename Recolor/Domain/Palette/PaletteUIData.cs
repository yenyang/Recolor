// <copyright file="PaletteUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

using Unity.Entities;

namespace Recolor.Domain.Palette
{
    public class PaletteUIData
    {
        public SwatchUIData[] m_Swatches;
        public Entity m_PrefabEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteUIData"/> class.
        /// </summary>
        public PaletteUIData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteUIData"/> class.
        /// </summary>
        /// <param name="prefabEntity">Palette Prefab Entity</param>
        /// <param name="swatches">Swatch colors.</param>
        /// <param name="subcategory">Subcategory name.</param>
        public PaletteUIData(Entity prefabEntity, SwatchUIData[] swatches)
        {
            m_PrefabEntity = prefabEntity;
            m_Swatches = swatches;
        }

        /// <summary>
        /// Gets or sets the Swatches.
        /// </summary>
        public SwatchUIData[] Swatches
        {
            get { return m_Swatches; }
            set { m_Swatches = value; }
        }

        /// <summary>
        /// Gets or sets the prefab entity.
        /// </summary>
        public Entity PrefabEntity
        {
            get { return m_PrefabEntity; }
            set { m_PrefabEntity = value; }
        }
    }
}
