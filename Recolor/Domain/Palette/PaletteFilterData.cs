// <copyright file="PaleteFilterData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Unity.Entities;

    /// <summary>
    /// Custom component for containing filters for palletes.
    /// </summary>
    public struct PaletteFilterData : IBufferElementData
    {
        /// <summary>
        /// Prefab entity for filtering visibility.
        /// </summary>
        public Entity m_PrefabEntity;

        /// <summary>
        /// Type of filter.
        /// </summary>
        public PaletteFilterType m_FilterType;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteFilterData"/> struct.
        /// </summary>
        /// <param name="prefabEntity">Prefab entity for filter.</param>
        /// <param name="filterType">Type of filter.</param>
        public PaletteFilterData(Entity prefabEntity, PaletteFilterType filterType)
        {
            m_FilterType = filterType;
            m_PrefabEntity = prefabEntity;
        }

        /// <summary>
        /// Type of filter for the pallete.
        /// </summary>
        public enum PaletteFilterType
        {
            /// <summary>
            /// Theme prefab filter.
            /// </summary>
            Theme = 0,

            /// <summary>
            /// Region pack prefab filter.
            /// </summary>
            Pack = 1,

            /// <summary>
            /// Zone prefab filter.
            /// </summary>
            ZoningType = 2,
        }
    }
}
