// <copyright file="PaletteFilterInfo.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Game.Prefabs;

    /// <summary>
    /// For swatches prefab filter generation.
    /// </summary>
    public class PaletteFilterInfo
    {
        /// <summary>
        /// Prefab for filter.
        /// </summary>
        public string m_FilterPrefabName;

        /// <summary>
        /// Type of filter.
        /// </summary>
        public PaletteFilterData.PaletteFilterType m_FilterType;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteFilterInfo"/> class.
        /// </summary>
        /// <param name="prefabBase">Prefab for filter.</param>
        /// <param name="filterType">Type of filter.</param>
        public PaletteFilterInfo(PrefabBase prefabBase, PaletteFilterData.PaletteFilterType filterType)
        {
            m_FilterPrefabName = prefabBase.name;
            m_FilterType = filterType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteFilterInfo"/> class.
        /// </summary>
        public PaletteFilterInfo()
        {
        }
    }
}
