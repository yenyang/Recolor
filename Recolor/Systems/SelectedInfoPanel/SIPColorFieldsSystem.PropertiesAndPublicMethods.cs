// <copyright file="SIPColorFieldsSystem.PropertiesAndPublicMethods.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.SelectedInfoPanel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Common;
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.Palettes;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    ///  Adds color fields to selected info panel for changing colors of buildings, vehicles, props, etc.
    /// </summary>
    public partial class SIPColorFieldsSystem : ExtendedInfoSectionBase
    {
        /// <summary>
        /// Gets a value indicating whether single instance is selected.
        /// </summary>
        public bool SingleInstance
        {
            get { return (m_SingleInstance.Value & ButtonState.On) == ButtonState.On; }
        }


        /// <summary>
        /// Gets or sets the copied color set.
        /// </summary>
        public ColorSet CopiedColorSet
        {
            get { return m_CopiedColorSet; }
            set { m_CopiedColorSet = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether CanPasteColorSet.
        /// </summary>
        public bool CanPasteColorSet
        {
            get { return m_CanPasteColorSet.Value; }
            set { m_CanPasteColorSet.Value = value; }
        }

        /// <summary>
        /// Gets the current entity.
        /// </summary>
        public Entity CurrentEntity
        {
            get { return m_CurrentEntity; }
        }

        /// <summary>
        /// Gets a the copied color set.
        /// </summary>
        public UnityEngine.Color CopiedColor
        {
            get { return m_CopiedColor; }
        }

        /// <summary>
        /// Gets or sets the current state.
        /// </summary>
        public State CurrentState
        {
            get { return m_State; }
            set { m_State = value; }
        }

        /// <inheritdoc/>
        protected override string group => Mod.Id;

        /// <inheritdoc/>
        protected override bool displayForUpgrades => true;

        /// <summary>
        /// Checks if the entire color set matches vanilla.
        /// </summary>
        /// <param name="colorSet">Color set for comparison.</param>
        /// <param name="assetSeasonIdentifier">Identifier.</param>
        /// <returns>True if matches entires, false if not.</returns>
        public bool MatchesEntireVanillaColorSet(ColorSet colorSet, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            bool[] matches = MatchesVanillaColorSet(colorSet, assetSeasonIdentifier);
            if (matches[0] && matches[1] && matches[2])
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets season from color group id using a loop and consistency with color group ids equally season. May need adjustment later.
        /// </summary>
        /// <param name="colorGroupID">Color group ID from color variation.</param>
        /// <param name="season">outputted season or spring if false.</param>
        /// <returns>true is converted, false if not.</returns>
        public bool TryGetSeasonFromColorGroupID(ColorGroupID colorGroupID, out Season season)
        {
            season = Season.None;
            for (int i = (int)Season.Spring; i <= (int)Season.Winter; i++)
            {
                if (colorGroupID == new ColorGroupID(i))
                {
                    season = (Season)i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if vanilla color set has been recorded and returns it in out if available.
        /// </summary>
        /// <param name="assetSeasonIdentifier">struct with necessary data.</param>
        /// <param name="colorSet">Color set returned through out parameter.</param>
        /// <returns>True if found. false if not.</returns>
        public bool TryGetVanillaColorSet(AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet colorSet)
        {
            colorSet = default;
            if (!m_VanillaColorSets.ContainsKey(assetSeasonIdentifier))
            {
                return false;
            }

            colorSet = m_VanillaColorSets[assetSeasonIdentifier];
            return true;
        }

        /// <summary>
        /// Gets AssetSeasonIdentifier and closes colorVariation color set as outs.
        /// </summary>
        /// <param name="entity">Selected Entity to get ASI for.</param>
        /// <param name="assetSeasonIdentifier">The out result with Asset Season Idenifier.</param>
        /// <param name="colorSet">The closest color variation set.</param>
        /// <returns>True if found, false if not.</returns>
        public bool TryGetAssetSeasonIdentifier(Entity entity, out AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet colorSet)
        {
            assetSeasonIdentifier = default;
            colorSet = default;
            bool foundClimatePrefab = true;
            if (m_ClimatePrefab is null)
            {
                foundClimatePrefab = GetClimatePrefab();
            }

            if (!foundClimatePrefab
                || !EntityManager.TryGetComponent(entity, out PrefabRef prefabRef)
                || !EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer)
                || !EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer)
                || !EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                m_Log.Debug("Early return.");
                return false;
            }

            ColorSet originalMeshColor = meshColorBuffer[0].m_ColorSet;
            if (EntityManager.TryGetComponent(entity, out Game.Objects.Tree tree))
            {
                if (tree.m_State == Game.Objects.TreeState.Dead || tree.m_State == Game.Objects.TreeState.Collected || tree.m_State == Game.Objects.TreeState.Stump)
                {
                    return false;
                }

                if ((int)tree.m_State > 0)
                {
                    originalMeshColor = meshColorBuffer[(int)Math.Log((int)tree.m_State, 2) + 1].m_ColorSet;
                }
            }

            Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.name);

            colorSet = colorVariationBuffer[0].m_ColorSet;
            int index = 0;
            float cummulativeDifference = float.MaxValue;
            Season season = Season.None;
            for (int i = 0; i < colorVariationBuffer.Length; i++)
            {
                if ((TryGetSeasonFromColorGroupID(colorVariationBuffer[i].m_GroupID, out Season checkSeason) &&
                     checkSeason == currentSeason) ||
                     checkSeason == Season.None)
                {
                    float currentCummulativeDifference = CalculateCummulativeDifference(originalMeshColor, colorVariationBuffer[i].m_ColorSet);
                    if (currentCummulativeDifference < cummulativeDifference)
                    {
                        cummulativeDifference = currentCummulativeDifference;
                        index = i;
                        colorSet = colorVariationBuffer[i].m_ColorSet;
                        season = checkSeason;
                    }
                }
            }

            if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[0].m_SubMesh, out PrefabBase prefabBase))
            {
                return false;
            }

            assetSeasonIdentifier = new AssetSeasonIdentifier()
            {
                m_Index = index,
                m_PrefabID = prefabBase.GetPrefabID(),
                m_Season = season,
            };
            return true;
        }

        /// <summary>
        /// Deletes all xml data files in ModsData and resets instances.
        /// </summary>
        public void DeleteAllModsDataFiles()
        {
            string[] filePaths = Directory.GetFiles(m_ContentFolder);
            NativeList<Entity> prefabsNeedingUpdates = new (Allocator.Temp);
            foreach (string filePath in filePaths)
            {
                SavedColorSet colorSet = default;
                if (File.Exists(filePath))
                {
                    try
                    {
                        XmlSerializer serTool = new (typeof(SavedColorSet)); // Create serializer
                        using (FileStream readStream = new (filePath, System.IO.FileMode.Open)) // Open
                        {
                            colorSet = (SavedColorSet)serTool.Deserialize(readStream);
                        }

                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        m_Log.Warn($"{nameof(SIPColorFieldsSystem)}.{nameof(ReloadSavedColorSetsFromDisk)} Could not deserialize file at {filePath}. Encountered exception {ex}");
                        continue;
                    }
                }

                PrefabID prefabID = new (colorSet.PrefabType, colorSet.PrefabName);

                if (!m_PrefabSystem.TryGetPrefab(prefabID, out PrefabBase prefabBase))
                {
                    continue;
                }

                if (!m_PrefabSystem.TryGetEntity(prefabBase, out Entity e))
                {
                    continue;
                }

                if (!EntityManager.TryGetBuffer(e, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer))
                {
                    continue;
                }

                for (int j = 0; j < colorVariationBuffer.Length; j++)
                {
#if VERBOSE
                m_Log.Verbose($"{prefabID.GetName()} {(TreeState)(int)Math.Pow(2, i - 1)} {(FoliageUtils.Season)j} {colorVariationBuffer[j].m_ColorSet.m_Channel0} {colorVariationBuffer[j].m_ColorSet.m_Channel2} {colorVariationBuffer[j].m_ColorSet.m_Channel2}");
#endif
                    ColorVariation currentColorVariation = colorVariationBuffer[j];
                    TryGetSeasonFromColorGroupID(currentColorVariation.m_GroupID, out Season season);

                    AssetSeasonIdentifier assetSeasonIdentifier = new()
                    {
                        m_PrefabID = prefabID,
                        m_Season = season,
                        m_Index = j,
                    };

                    if (!m_CustomColorVariationSystem.TryGetCustomColorVariation(e, j, out CustomColorVariations customColorVariation) &&
                        TryGetVanillaColorSet(assetSeasonIdentifier, out currentColorVariation.m_ColorSet))
                    {
                        colorVariationBuffer[j] = currentColorVariation;
                        if (!prefabsNeedingUpdates.Contains(e))
                        {
                            prefabsNeedingUpdates.Add(e);
                        }

                        m_Log.Debug($"{nameof(SIPColorFieldsSystem)}.{nameof(DeleteAllModsDataFiles)} Reset Colorset for {prefabID} in {assetSeasonIdentifier.m_Season}");
                    }
                }
            }

            if (prefabsNeedingUpdates.Length == 0)
            {
                return;
            }

            EntityQuery prefabRefQuery = SystemAPI.QueryBuilder()
                .WithAll<PrefabRef>()
                .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                .Build();

            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();

            NativeArray<Entity> entities = prefabRefQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in entities)
            {
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) &&
                    EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer) &&
                    prefabsNeedingUpdates.Contains(currentSubMeshBuffer[0].m_SubMesh))
                {
                    buffer.AddComponent<BatchesUpdated>(e);
                }
            }
        }

        /// <summary>
        /// Adds batches updated to subelements.
        /// </summary>
        /// <param name="e">owner Entity.</param>
        /// <param name="buffer">Entity command buffer for appropriate phase.</param>
        public void AddBatchesUpdatedToSubElements(Entity e, EntityCommandBuffer buffer)
        {
            // Add batches updated to subobjects that have mesh color.
            if (EntityManager.TryGetBuffer(e, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> subObjects))
            {
                foreach (Game.Objects.SubObject subObject in subObjects)
                {
                    if (EntityManager.HasBuffer<MeshColor>(subObject.m_SubObject))
                    {
                        buffer.AddComponent<BatchesUpdated>(subObject.m_SubObject);
                    }

                    if (!EntityManager.TryGetBuffer(subObject.m_SubObject, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> subObjects2))
                    {
                        continue;
                    }

                    foreach (Game.Objects.SubObject subObject2 in subObjects2)
                    {
                        if (EntityManager.HasBuffer<MeshColor>(subObject2.m_SubObject))
                        {
                            buffer.AddComponent<BatchesUpdated>(subObject2.m_SubObject);
                        }

                        if (!EntityManager.TryGetBuffer(subObject2.m_SubObject, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> subObjects3))
                        {
                            continue;
                        }

                        foreach (Game.Objects.SubObject subObject3 in subObjects3)
                        {
                            if (EntityManager.HasBuffer<MeshColor>(subObject3.m_SubObject))
                            {
                                buffer.AddComponent<BatchesUpdated>(subObject3.m_SubObject);
                            }

                            if (!EntityManager.TryGetBuffer(subObject3.m_SubObject, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> subObjects4))
                            {
                                continue;
                            }

                            foreach (Game.Objects.SubObject subObject4 in subObjects4)
                            {
                                if (EntityManager.HasBuffer<MeshColor>(subObject4.m_SubObject))
                                {
                                    buffer.AddComponent<BatchesUpdated>(subObject4.m_SubObject);
                                }
                            }
                        }
                    }
                }
            }

            // Add batches updated to sublanes that have mesh color.
            if (EntityManager.TryGetBuffer(e, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> subLanes))
            {
                foreach (Game.Net.SubLane subLane in subLanes)
                {
                    if (EntityManager.HasBuffer<MeshColor>(subLane.m_SubLane))
                    {
                        buffer.AddComponent<BatchesUpdated>(subLane.m_SubLane);
                    }
                }
            }
        }

        /// <summary>
        /// Resets a single instance color change by channel.
        /// </summary>
        /// <param name="channel">Channel 0 - 2.</param>
        /// <param name="entity">Instance Entity.</param>
        /// <param name="buffer">ECB from appropriate phase.</param>
        public void ResetSingleInstanceByChannel(int channel, Entity entity, EntityCommandBuffer buffer)
        {
            bool removeComponents = true;


            if (EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MeshColorRecord> meshColorRecordBuffer) &&
                EntityManager.TryGetBuffer(entity, isReadOnly: false, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer) &&
                channel >= 0 && channel <= 2)
            {
                for (int i = 0; i < m_SubMeshData.Value.SubMeshLength; i++)
                {
                    CustomMeshColor customMeshColor = customMeshColorBuffer[i];
                    if (m_SubMeshIndexes.Contains(i) ||
                        m_ServiceVehicles.Value == ButtonState.On ||
                        m_Route.Value == ButtonState.On)
                    {
                        customMeshColor.m_ColorSet[channel] = meshColorRecordBuffer[i].m_ColorSet[channel];
                        customMeshColorBuffer[i] = customMeshColor;
                    }

                    if (!MatchesEntireVanillaColorSet(meshColorRecordBuffer[i].m_ColorSet, customMeshColor.m_ColorSet))
                    {
                        removeComponents = false;
                    }
                }
            }
            else
            {
                removeComponents = true;
            }

            if (removeComponents)
            {
                buffer.RemoveComponent<CustomMeshColor>(entity);
                buffer.RemoveComponent<MeshColorRecord>(entity);
            }

            buffer.AddComponent<BatchesUpdated>(entity);

            AddBatchesUpdatedToSubElements(entity, buffer);
        }
    }
}
