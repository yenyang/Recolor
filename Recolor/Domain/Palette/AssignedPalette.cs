// <copyright file="PaletteAssignment.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Colossal.Serialization.Entities;
    using Unity.Entities;

    /// <summary>
    /// Buffer component for assigned Palette to an entity.
    /// </summary>
    [InternalBufferCapacity(3)]
    public struct AssignedPalette : IBufferElementData, ISerializable, IQueryTypeParameter
    {
        /// <summary>
        /// Channel this palette applies to.
        /// </summary>
        public int m_Channel;

        /// <summary>
        /// Palette Prefab Entity.
        /// </summary>
        public Entity m_PaletteInstanceEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssignedPalette"/> struct.
        /// </summary>
        /// <param name="channel">Channel 0-2.</param>
        /// <param name="paletteInstanceEntity">Prefab Entity.</param>
        public AssignedPalette(int channel,  Entity paletteInstanceEntity)
        {
            m_Channel = channel;
            m_PaletteInstanceEntity = paletteInstanceEntity;
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1);
            writer.Write(m_PaletteInstanceEntity);
            writer.Write(m_Channel);
        }

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int _);
            reader.Read(out m_PaletteInstanceEntity);
            reader.Read(out m_Channel);
        }
    }
}
