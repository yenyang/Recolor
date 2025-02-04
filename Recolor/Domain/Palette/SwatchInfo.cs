// <copyright file="SwatchInfo.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Game.Prefabs;
    using System.Collections.Generic;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Class for swatch prefabs.
    /// </summary>
    public class SwatchInfo
    {
        /// <summary>
        /// The set of 3 colors.
        /// </summary>
        public Color m_SwatchColor;

        /// <summary>
        /// The probability weight.
        /// </summary>
        public int m_ProbabilityWeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchInfo"/> class.
        /// </summary>
        /// <param name="color">Color for the swatch.</param>
        /// <param name="probabilityWeight">Weight for likelyhood color will appear.</param>
        public SwatchInfo(Color color, int probabilityWeight)
        {
            m_SwatchColor = color;
            m_ProbabilityWeight = probabilityWeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchInfo"/> class.
        /// </summary>
        /// <param name="swatchUIData">Data for UI.</param>
        public SwatchInfo(SwatchUIData swatchUIData)
        {
            m_SwatchColor = swatchUIData.SwatchColor;
            m_ProbabilityWeight = swatchUIData.ProbabilityWeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchInfo"/> class.
        /// </summary>
        public SwatchInfo()
        {
        }
    }
}
