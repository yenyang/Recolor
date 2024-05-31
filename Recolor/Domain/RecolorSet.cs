// <copyright file="RecolorSet.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain
{
    using Game.Rendering;
    using UnityEngine;

    /// <summary>
    /// A class for moving color data into and out of UI.
    /// </summary>
    public class RecolorSet
    {
        public Color Channel0;
        public Color Channel1;
        public Color Channel2;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecolorSet"/> class.
        /// </summary>
        /// <param name="colorSet">Game.Rendering colorset.</param>
        public RecolorSet(ColorSet colorSet)
        {
            Channel0 = colorSet.m_Channel0;
            Channel1 = colorSet.m_Channel1;
            Channel2 = colorSet.m_Channel2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecolorSet"/> class.
        /// </summary>
        /// <param name="color0">First color.</param>
        /// <param name="color1">2nd color.</param>
        /// <param name="color2">3rd color.</param>
        public RecolorSet(Color color0, Color color1, Color color2)
        {
            Channel0 = color0;
            Channel1 = color1;
            Channel2 = color2;
        }

        /// <summary>
        ///  Gets a vanilla color set.
        /// </summary>
        /// <returns>Vanilla Color set.</returns>
        public ColorSet GetColorSet()
        {
            ColorSet colorSet = new ()
            {
                m_Channel0 = Channel0,
                m_Channel1 = Channel1,
                m_Channel2 = Channel2,
            };
            return colorSet;
        }
    }
}
