// <copyright file="Swatch.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Colossal.Serialization.Entities;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Custom component for containing color and probability information for a swatch on an instance entity.
    /// </summary>
    public struct Swatch : IBufferElementData, ISerializable
    {
        /// <summary>
        /// The swatch color.
        /// </summary>
        public Color m_SwatchColor;

        /// <summary>
        /// The probability weight.
        /// </summary>
        public int m_ProbabilityWeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="Swatch"/> struct.
        /// </summary>
        /// <param name="color">Color for the swatch.</param>
        /// <param name="probabilityWeight">Weight for likelyhood color will appear.</param>
        public Swatch(Color color, int probabilityWeight)
        {
            m_SwatchColor = color;
            m_ProbabilityWeight = probabilityWeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Swatch"/> struct.
        /// </summary>
        /// <param name="swatchInfo">Generate swatchData from swatch info.</param>
        public Swatch(SwatchInfo swatchInfo)
        {
            m_SwatchColor = swatchInfo.GetColor();
            m_ProbabilityWeight = swatchInfo.m_ProbabilityWeight;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Swatch"/> struct.
        /// </summary>
        /// <param name="swatchData">SwatchData buffer component from Prefab Entity.</param>
        public Swatch(SwatchData swatchData)
        {
            m_SwatchColor = swatchData.m_SwatchColor;
            m_ProbabilityWeight = swatchData.m_ProbabilityWeight;
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1); // version
            writer.Write(m_SwatchColor);
            writer.Write(m_ProbabilityWeight);
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int _);
            reader.Read(out m_SwatchColor);
            reader.Read(out m_ProbabilityWeight);
        }
    }
}
