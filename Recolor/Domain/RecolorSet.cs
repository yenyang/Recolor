// <copyright file="RecolorSet.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain
{
    using Game.Rendering;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// A class for moving color data into and out of UI.
    /// </summary>
    public class RecolorSet
    {
        /// <summary>
        /// First color channel.
        /// </summary>
        public Color[] Channels;

        /// <summary>
        /// True for on, false for off.
        /// </summary>
        public bool[] States;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecolorSet"/> class.
        /// </summary>
        /// <param name="colorSet">Game.Rendering colorset.</param>
        public RecolorSet(ColorSet colorSet)
        {
            Channels = new Color[] { colorSet.m_Channel0, colorSet.m_Channel1, colorSet.m_Channel2 };
            States = new bool[3] { true, true, true };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecolorSet"/> class.
        /// </summary>
        /// <param name="color0">First color.</param>
        /// <param name="color1">2nd color.</param>
        /// <param name="color2">3rd color.</param>
        public RecolorSet(Color color0, Color color1, Color color2)
        {
            Channels = new Color[] { color0, color1, color2 };
            States = new bool[3] { true, true, true };
        }

        /// <summary>
        ///  Gets a vanilla color set.
        /// </summary>
        /// <returns>Vanilla Color set.</returns>
        public ColorSet GetColorSet()
        {
            ColorSet colorSet = new ()
            {
                m_Channel0 = Channels[0],
                m_Channel1 = Channels[1],
                m_Channel2 = Channels[2],
            };
            return colorSet;
        }

        /// <summary>
        /// Sets the color set.
        /// </summary>
        /// <param name="colorSet">Game.Rendering.ColorSet.</param>
        public void SetColorSet(ColorSet colorSet)
        {
            Channels = new Color[] { colorSet.m_Channel0, colorSet.m_Channel1, colorSet.m_Channel2 };
        }

        /// <summary>
        /// If valid channel is supplied, toggles state of that channel to opposite.
        /// </summary>
        /// <param name="channel">0, 1 or 2.</param>
        public void ToggleChannel(uint channel)
        {
            if (channel < States.Length)
            {
                States[channel] = !States[channel];
            }
        }

        /// <summary>
        /// Gets a bool3 of the channel toggles.
        /// </summary>
        /// <returns>Bool3 for channel toggles.</returns>
        public bool3 GetChannelToggles()
        {
            if (States.Length >= 3)
            {
                return new bool3(States[0], States[1], States[2]);
            }
            else
            {
                return new bool3(true, true, true);
            }
        }
    }
}
