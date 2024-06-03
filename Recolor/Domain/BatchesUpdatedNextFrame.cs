// <copyright file="BatchesUpdatedNextFrame.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain
{
    using Unity.Entities;

    /// <summary>
    /// A component used to add batches updated on the next frame.
    /// </summary>
    public struct BatchesUpdatedNextFrame : IComponentData, IQueryTypeParameter
    {
    }
}
