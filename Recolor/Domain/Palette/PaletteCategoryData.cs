// <copyright file="PaletteCategoryData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Unity.Entities;

    /// <summary>
    /// Custom component for pallete cateogory that controls visibility.
    /// </summary>
    public struct PaletteCategoryData : IComponentData, IQueryTypeParameter
    {
        /// <summary>
        /// Assigns a category to the pallete that controls when it is visible.
        /// </summary>
        public PaletteCategory m_Category;

        /// <summary>
        /// A prefab entity for a subcategory.
        /// </summary>
        public Entity m_SubCategory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteCategoryData"/> struct.
        /// </summary>
        /// <param name="category">Top level Category for filtering visibilty.</param>
        /// <param name="subCategory">SubCategory entity for organization.</param>
        public PaletteCategoryData(PaletteCategory category, Entity subCategory)
        {
            m_Category = category;
            m_SubCategory = subCategory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteCategoryData"/> struct.
        /// </summary>
        /// <param name="category">Top level Category for filtering visibilty.</param>
        public PaletteCategoryData(PaletteCategory category)
        {
            m_Category = category;
            m_SubCategory = Entity.Null;
        }

        /// <summary>
        /// Top level categories for palletes.
        /// </summary>
        public enum PaletteCategory
        {
            /// <summary>
            /// Default will apply to all types of assets.
            /// </summary>
            Any = 0,

            /// <summary>
            /// Limits to buildings.
            /// </summary>
            Buildings = 1,

            /// <summary>
            /// Limits to vehicles.
            /// </summary>
            Vehicles = 2,

            /// <summary>
            /// Limits to props.
            /// </summary>
            Props = 4,
        }
    }
}
