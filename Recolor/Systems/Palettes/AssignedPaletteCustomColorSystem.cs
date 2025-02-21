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
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

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

        /// <summary>
        /// Tries to get the randomized color from the palette.
        /// </summary>
        /// <param name="instanceEntity">Entity to change color of.</param>
        /// <param name="channel">Channel to change.</param>
        /// <param name="color">output color.</param>
        /// <returns>True if valid color. False if not.</returns>
        public bool TryGetColorFromPalette(Entity instanceEntity, int channel, out UnityEngine.Color color)
        {
            if (!EntityManager.TryGetBuffer(instanceEntity, isReadOnly: true, out DynamicBuffer<AssignedPalette> palettes) ||
                     palettes.Length == 0 ||
                    !EntityManager.TryGetComponent(instanceEntity, out PseudoRandomSeed pseudoRandomSeed) ||
                    !EntityManager.TryGetBuffer(instanceEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer) ||
                     meshColorBuffer.Length == 0 ||
                     channel < 0 ||
                     channel > 2)
            {
                color = default;
                return false;
            }

            for (int i = 0; i < palettes.Length; i++)
            {
                if (!EntityManager.TryGetBuffer(palettes[i].m_PaletteInstanceEntity, isReadOnly: true, out DynamicBuffer<Swatch> swatches) ||
                    palettes[i].m_Channel != channel)
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

                KeyValuePair<Color, int>[] weightedPrefabs = probabilityWeights.ToArray();

                int probabilityResult = GetProbabilityResult(totalProbabilityWeight, pseudoRandomSeed.m_Seed, channel);

                for (int j = 0; j < weightedPrefabs.Length; j++)
                {
                    if (weightedPrefabs[j].Value > probabilityResult)
                    {
                        color = weightedPrefabs[j].Key;
                        return true;
                    }
                    else
                    {
                        probabilityResult -= weightedPrefabs[j].Value;
                    }
                }
            }


            color = default;
            return false;
        }

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
                  .WithNone<Deleted, Temp, Game.Objects.Plant>()
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

                    KeyValuePair<UnityEngine.Color, int>[] weightedPrefabs = probabilityWeights.ToArray();

                    int probabilityResult = GetProbabilityResult(totalProbabilityWeight, pseudoRandomSeed.m_Seed, palettes[i].m_Channel);

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

        /// <summary>
        /// This method controls the final color for all palettes. After release DO NOT CHANGE THIS!.
        /// </summary>
        /// <param name="totalProbabilityWeight">Total weight of all swatch probabilities.</param>
        /// <param name="seed">PsudorandomSeed.m_seed</param>
        /// <param name="channel">channel 0-2.</param>
        /// <returns>Random Int between 0 and probability weight that should be sufficiently different between the 3 channels.</returns>
        private int GetProbabilityResult(int totalProbabilityWeight, ushort seed, int channel)
        {
            Unity.Mathematics.Random random = new (seed);
            for (int j = 0; j < channel * 10; j++)
            {
                random.NextInt(totalProbabilityWeight);
            }

            return random.NextInt(totalProbabilityWeight);
        }
    }
}
