﻿// <copyright file="CustomColorVariation.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain
{
    using Colossal.Serialization.Entities;
    using Game.Rendering;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// RETIRED Custom component for saving custom color variations into save file. Needed for migration.
    /// </summary>
    public struct CustomColorVariation : IComponentData, ISerializable
    {
        /// <summary>
        /// The set of 3 colors.
        /// </summary>
        public ColorSet m_ColorSet;

        /// <summary>
        /// The color variation index.
        /// </summary>
        public int m_Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomColorVariation"/> struct.
        /// </summary>
        /// <param name="colorSet">Set of colors.</param>
        /// <param name="index">Submesh index.</param>
        public CustomColorVariation(ColorSet colorSet, int index)
        {
            m_ColorSet = colorSet;
            m_Index = index;
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int _); // version
            reader.Read(out Color color0);
            reader.Read(out Color color1);
            reader.Read(out Color color2);
            m_ColorSet = new ColorSet
            {
                m_Channel0 = color0,
                m_Channel1 = color1,
                m_Channel2 = color2,
            };
            reader.Read(out m_Index);
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(0); // version
            writer.Write(m_ColorSet.m_Channel0);
            writer.Write(m_ColorSet.m_Channel1);
            writer.Write(m_ColorSet.m_Channel2);
            writer.Write(m_Index);
        }
    }
}
