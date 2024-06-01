// <copyright file="CustomMeshColor.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain
{
    using Colossal.Serialization.Entities;
    using Game.Rendering;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Used to record what the user wanted for their custom mesh color.
    /// </summary>
    [InternalBufferCapacity(1)]
    public struct CustomMeshColor : IBufferElementData, ISerializable
    {
        /// <summary>
        /// A color set for the custom mesh coloring.
        /// </summary>
        public ColorSet m_ColorSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMeshColor"/> struct.
        /// </summary>
        /// <param name="colorSet">set of three colors.</param>
        public CustomMeshColor(ColorSet colorSet)
        {
            m_ColorSet = colorSet;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMeshColor"/> struct.
        /// </summary>
        /// <param name="meshColor">original mesh color.</param>
        public CustomMeshColor(MeshColor meshColor)
        {
            m_ColorSet = meshColor.m_ColorSet;
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out Color color0);
            reader.Read(out Color color1);
            reader.Read(out Color color2);
            m_ColorSet = new ColorSet
            {
                m_Channel0 = color0,
                m_Channel1 = color1,
                m_Channel2 = color2,
            };
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(m_ColorSet.m_Channel0);
            writer.Write(m_ColorSet.m_Channel1);
            writer.Write(m_ColorSet.m_Channel2);
        }
    }
}
