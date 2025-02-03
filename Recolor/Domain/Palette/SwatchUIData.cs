// <copyright file="SwatchUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using UnityEngine;

    /// <summary>
    /// Class for swatch prefabs.
    /// </summary>
    public class SwatchUIData
    {
        private Color m_SwatchColor;
        private int m_ProbabilityWeight;
        private int m_Index;

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
        public SwatchUIData(Color color, int probabilityWeight, int index)
        {
            m_SwatchColor = color;
            m_ProbabilityWeight = probabilityWeight;
            m_Index = index;
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

        /// <summary>
        /// Gets or sets the index for the swatch.
        /// </summary>
        public int Index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }
    }
}
