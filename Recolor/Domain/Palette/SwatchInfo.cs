// <copyright file="SwatchInfo.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using UnityEngine;

    /// <summary>
    /// Class for swatch prefabs.
    /// </summary>
    public class PaletteInfo
    {
        /// <summary>
        /// The set of 3 colors.
        /// </summary>
        public Color m_Color;

        /// <summary>
        /// The probability weight.
        /// </summary>
        public int m_ProbabilityWeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteInfo"/> class.
        /// </summary>
        /// <param name="color">Color for the swatch.</param>
        /// <param name="probabilityWeight">Weight for likelyhood color will appear.</param>
        public PaletteInfo(Color color, int probabilityWeight)
        {
            m_Color = color;
            m_ProbabilityWeight = probabilityWeight;
        }
    }
}
