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
    public struct AssignedPalette : IBufferElementData, ISerializable
    {
        /// <summary>
        /// Channel this palette applies to.
        /// </summary>
        public int m_Channel;

        /// <summary>
        /// Palette Prefab Entity.
        /// </summary>
        public Entity m_PrefabEntity;

        /// <inheritdoc/>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int _);
            reader.Read(out m_PrefabEntity);
        }

        /// <inheritdoc/>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1);
            writer.Write(m_PrefabEntity);
        }
    }
}
