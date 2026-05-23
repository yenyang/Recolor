// <copyright file="AppliedPaletteCleanup.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain.Palette;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Removed Recolor.Domain.CustomMeshColor from applied entities with assigned palettes during placement.
    /// </summary>
    public partial class AppliedPaletteCleanup : GameSystemBase

    {
        private EntityQuery m_AppliedAndAssignedPaletteQuery;
        private ILog m_Log;
        private ModificationBarrier1 m_Barrier;
 
        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;

            m_Barrier = World.GetOrCreateSystemManaged<ModificationBarrier1>();

            m_AppliedAndAssignedPaletteQuery = SystemAPI.QueryBuilder()
                  .WithAll<Applied, AssignedPalette, PseudoRandomSeed, MeshColor>()
                  .WithNone<Deleted, Game.Objects.Plant, Game.Creatures.Creature>()
                  .Build();

            RequireForUpdate(m_AppliedAndAssignedPaletteQuery);
            m_Log.Info($"{nameof(AppliedPaletteCleanup)}.{nameof(OnCreate)}");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_AppliedAndAssignedPaletteQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();
            buffer.RemoveComponent<Domain.CustomMeshColor>(entities);
        }
    }
}
