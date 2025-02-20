// <copyright file="AssignedPaletteCustomColorSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System.Collections.Generic;
    using System.Linq;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// System for assigning custom mesh colors to entities with Assigned palettes.
    /// </summary>
    public partial class AssignedPaletteCustomColorSystem : GameSystemBase
    {
        private EntityQuery m_AssignedPaletteQuery;
        private ILog m_Log;
        private ColorPainterToolSystem m_ColorPainterToolSystem;
        private EndFrameBarrier m_Barrier;
        private SIPColorFieldsSystem m_SIPColorFieldsSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(AssignedPaletteCustomColorSystem)}.{nameof(OnCreate)}");

            m_ColorPainterToolSystem = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_Barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SIPColorFieldsSystem = World.GetOrCreateSystemManaged<SIPColorFieldsSystem>();

            m_AssignedPaletteQuery = SystemAPI.QueryBuilder()
                  .WithAll<AssignedPalette, PseudoRandomSeed, MeshColor, BatchesUpdated>()
                  .WithNone<Deleted, Temp, Plant>()
                  .Build();

            RequireForUpdate(m_AssignedPaletteQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_AssignedPaletteQuery.ToEntityArray(Allocator.Temp);
            EntityCommandBuffer buffer = m_Barrier.CreateCommandBuffer();

            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<AssignedPalette> palettes) ||
                     palettes.Length == 0 ||
                    !EntityManager.TryGetComponent(entity, out PseudoRandomSeed pseudoRandomSeed) ||
                    !EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) ||
                     meshColorBuffer.Length == 0)
                {
                    continue;
                }

                ColorSet colorSet = meshColorBuffer[0].m_ColorSet;
                Unity.Mathematics.Random random = new (pseudoRandomSeed.m_Seed);
                for (int i = 0; i < palettes.Length; i++)
                {
                    if (!EntityManager.TryGetBuffer(palettes[i].m_PaletteInstanceEntity, isReadOnly: true, out DynamicBuffer<Swatch> swatches))
                    {
                        continue;
                    }

                    int totalProbabilityWeight = 0;
                    Dictionary<UnityEngine.Color, int> probabilityWeights = new Dictionary<UnityEngine.Color, int>();
                    foreach (Swatch swatch in swatches)
                    {
                        if (!probabilityWeights.ContainsKey(swatch.m_SwatchColor))
                        {
                            probabilityWeights.Add(swatch.m_SwatchColor, swatch.m_ProbabilityWeight);
                        }
                        else
                        {
                            probabilityWeights[swatch.m_SwatchColor] += swatch.m_ProbabilityWeight;
                        }

                        totalProbabilityWeight += swatch.m_ProbabilityWeight;
                    }

                    if (totalProbabilityWeight == 0)
                    {
                        continue;
                    }

                    int iterations = random.NextInt(10);
                    for (int j = 0; j < iterations; j++)
                    {
                        random.NextInt();
                    }

                    KeyValuePair<UnityEngine.Color, int>[] weightedPrefabs = probabilityWeights.ToArray();
                    int probabilityResult = random.NextInt(totalProbabilityWeight);

                    for (int j = 0; j < weightedPrefabs.Length; j++)
                    {
                        if (weightedPrefabs[j].Value > probabilityResult)
                        {
                            colorSet[palettes[i].m_Channel] = weightedPrefabs[j].Key;
                            break;
                        }
                        else
                        {
                            probabilityResult -= weightedPrefabs[j].Value;
                        }
                    }
                }

                m_ColorPainterToolSystem.ChangeInstanceColorSet(new RecolorSet(colorSet), ref buffer, entity);
            }
        }
    }
}
