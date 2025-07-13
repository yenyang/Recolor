// <copyright file="PaletteSubcategoryData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Unity.Entities;
    using static Recolor.Domain.Palette.PaletteCategoryData;

    /// <summary>
    /// Custom component for subcategoriess of palletes.
    /// </summary>
    public struct PaletteSubcategoryData : IComponentData, IQueryTypeParameter
    {
        /// <summary>
        /// Assigns a category to the pallete that controls when it is visible.
        /// </summary>
        public PaletteCategory m_Category;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteSubcategoryData"/> struct.
        /// </summary>
        /// <param name="category">Category this subcategory belongs to.</param>
        public PaletteSubcategoryData(PaletteCategory category)
        {
            m_Category = category;
        }
    }
}
