// <copyright file="SwatchData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Custom component for containing color and probability information for a swatch.
    /// </summary>
    public struct SwatchData : IBufferElementData
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
        /// Initializes a new instance of the <see cref="SwatchData"/> struct.
        /// </summary>
        /// <param name="color">Color for the swatch.</param>
        /// <param name="probabilityWeight">Weight for likelyhood color will appear.</param>
        public SwatchData(Color color, int probabilityWeight)
        {
            m_Color = color;
            m_ProbabilityWeight = probabilityWeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchData"/> struct.
        /// </summary>
        /// <param name="swatchInfo">Generate swatchData from swatch info.</param>
        public SwatchData(PaletteInfo swatchInfo)
        {
            m_Color = swatchInfo.m_Color;
            m_ProbabilityWeight = swatchInfo.m_ProbabilityWeight;
        }
    }
}
