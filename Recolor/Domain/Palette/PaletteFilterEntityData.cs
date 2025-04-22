// <copyright file="PaletteFilterEntityData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Unity.Entities;

    /// <summary>
    /// Custom component for containing filters for palletes.
    /// </summary>
    public struct PaletteFilterEntityData : IBufferElementData
    {
        /// <summary>
        /// Prefab entity for filtering visibility.
        /// </summary>
        public Entity m_PrefabEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteFilterEntityData"/> struct.
        /// </summary>
        /// <param name="prefabEntity">Prefab entity for filter.</param>
        /// <param name="filterType">Type of filter.</param>
        public PaletteFilterEntityData(Entity prefabEntity)
        {
            m_PrefabEntity = prefabEntity;
        }
    }
}
