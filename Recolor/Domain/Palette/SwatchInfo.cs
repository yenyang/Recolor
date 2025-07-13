// <copyright file="SwatchInfo.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using UnityEngine;

    /// <summary>
    /// Class for swatch prefabs.
    /// </summary>
    public class SwatchInfo
    {
        public float m_Red;
        public float m_Green;
        public float m_Blue;
        public float m_Alpha;

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
            AssignColorValues(color);
            m_ProbabilityWeight = probabilityWeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchInfo"/> class.
        /// </summary>
        public SwatchInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchInfo"/> class.
        /// </summary>
        /// <param name="swatchUIData">Data for UI.</param>
        public SwatchInfo(SwatchUIData swatchUIData)
        {
            AssignColorValues(swatchUIData.SwatchColor);
            m_ProbabilityWeight = swatchUIData.ProbabilityWeight;
        }

        /// <summary>
        /// Assigns the color values from UnityEngine.Color
        /// </summary>
        /// <param name="color">Color swatch color.</param>
        public void AssignColorValues(Color color)
        {
            m_Red = color.r;
            m_Green = color.g;
            m_Blue = color.b;
            m_Alpha = color.a;
        }

        /// <summary>
        /// Gets the UnityEngine.Color for swatch.
        /// </summary>
        /// <returns>UnityEngine.Color.</returns>
        public Color GetColor()
        {
            return new UnityEngine.Color(m_Red, m_Green, m_Blue, m_Alpha);
        }
    }
}
