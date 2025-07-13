// <copyright file="PaletteUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

using Unity.Entities;

namespace Recolor.Domain.Palette
{
    /// <summary>
    /// A class for handling data transfer for palettes.
    /// </summary>
    public class PaletteUIData
    {
        public SwatchUIData[] m_Swatches;
        public Entity m_PrefabEntity;
        public string m_PrefabName;

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
        public PaletteUIData(Entity prefabEntity, SwatchUIData[] swatches, string prefabName)
        {
            m_PrefabEntity = prefabEntity;
            m_Swatches = swatches;
            m_PrefabName = prefabName;
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

        /// <summary>
        /// Gets the name key.
        /// </summary>
        public string NameKey
        {
            get { return $"{Mod.Id}.{nameof(Systems.Palettes.PalettesUISystem.MenuType.Palette)}.NAME[{m_PrefabName}]"; }
        }

        /// <summary>
        /// Gets the description key.
        /// </summary>
        public string DescriptionKey
        {
            get { return $"{Mod.Id}.{nameof(Systems.Palettes.PalettesUISystem.MenuType.Palette)}.DESCRIPTION[{m_PrefabName}]"; }
        }

        /// <summary>
        /// Gets or sets the prefab name.
        /// </summary>
        public string Name
        {
            get { return m_PrefabName; }
            set { m_PrefabName = value; }
        }
    }
}
