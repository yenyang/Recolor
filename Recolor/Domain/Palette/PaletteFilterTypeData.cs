// <copyright file="PaletteFilterTypeData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Unity.Entities;

    /// <summary>
    /// Custom component for containing filter types for palletes.
    /// </summary>
    public struct PaletteFilterTypeData : IComponentData
    {
        /// <summary>
        /// Type of filter.
        /// </summary>
        public PaletteFilterType m_FilterType;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteFilterTypeData"/> struct.
        /// </summary>
        /// <param name="prefabEntity">Prefab entity for filter.</param>
        /// <param name="filterType">Type of filter.</param>
        public PaletteFilterTypeData(PaletteFilterType filterType)
        {
            m_FilterType = filterType;
        }

        /// <summary>
        /// Type of filter for the pallete.
        /// </summary>
        public enum PaletteFilterType
        {
            /// <summary>
            /// No filter.
            /// </summary>
            None = 0,

            /// <summary>
            /// Theme prefab filter.
            /// </summary>
            Theme = 1,

            /// <summary>
            /// Region pack prefab filter.
            /// </summary>
            Pack = 2,

            /// <summary>
            /// Zone prefab filter.
            /// </summary>
            ZoningType = 3,
        }
    }
}
