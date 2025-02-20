// <copyright file="SwatchUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using UnityEngine;

    /// <summary>
    /// Class for swatch data transfer with ui.
    /// </summary>
    public class SwatchUIData
    {
        private Color m_SwatchColor;
        private int m_ProbabilityWeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchUIData"/> class.
        /// </summary>
        public SwatchUIData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchUIData"/> class.
        /// </summary>
        /// <param name="color">Color for the swatch.</param>
        /// <param name="probabilityWeight">Weight for likelyhood color will appear.</param>
        /// <param name="index">Index within buffer.</param>
        public SwatchUIData(Color color, int probabilityWeight)
        {
            m_SwatchColor = color;
            m_ProbabilityWeight = probabilityWeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwatchUIData"/> class.
        /// </summary>
        /// <param name="swatchData">Buffer component from prefab entity.</param>
        /// <param name="index">Index from the buffer.</param>
        public SwatchUIData(SwatchData swatchData)
        {
            m_SwatchColor = swatchData.m_SwatchColor;
            m_ProbabilityWeight = swatchData.m_ProbabilityWeight;
        }

        /// <summary>
        /// Gets or sets the Swatch color.
        /// </summary>
        public Color SwatchColor
        {
            get { return m_SwatchColor; }
            set { m_SwatchColor = value; }
        }

        /// <summary>
        /// Gets or sets the probability weight.
        /// </summary>/// <summary>
        public int ProbabilityWeight
        {
            get { return m_ProbabilityWeight; }
            set { m_ProbabilityWeight = value; }
        }
    }
}
