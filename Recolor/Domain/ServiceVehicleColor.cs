// <copyright file="ServiceVehicleColor.cs" company="Yenyang's Mods. MIT License">
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
    public struct ServiceVehicleColor : IBufferElementData, ISerializable
    {
        /// <summary>
        /// A color set for the custom mesh coloring.
        /// </summary>
        public ColorSet m_ColorSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceVehicleColor"/> struct.
        /// </summary>
        /// <param name="colorSet">set of three colors.</param>
        public ServiceVehicleColor(ColorSet colorSet)
        {
            m_ColorSet = colorSet;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceVehicleColor"/> struct.
        /// </summary>
        /// <param name="meshColor">original mesh color.</param>
        public ServiceVehicleColor(MeshColor meshColor)
        {
            m_ColorSet = meshColor.m_ColorSet;
        }

        /// <summary>
        /// Evaluates whether the <see cref="ServiceVehicleColor"/> equals a color set.
        /// </summary>
        /// <param name="colorSet">Color set to compare.</param>
        /// <returns>true if equal, false if not.</returns>
        public bool Equals(ColorSet colorSet)
        {
            for (int i = 0; i < 3; i++)
            {
                if (m_ColorSet[i] != colorSet[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int _); // version;
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
            writer.Write(1); // version;
            writer.Write(m_ColorSet.m_Channel0);
            writer.Write(m_ColorSet.m_Channel1);
            writer.Write(m_ColorSet.m_Channel2);
        }
    }
}
