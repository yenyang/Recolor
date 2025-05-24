// <copyright file="SIPColorFieldsSystem.ColorVariationPrivateMethods.cs" company="Yenyang's Mods. MIT License">
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
        private void GenerateOrUpdateCustomColorVariationEntity()
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
            if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
            {
                return;
            }

            ColorSet colorSet = colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index].m_ColorSet;
            if (!EntityManager.HasComponent<Game.Objects.Tree>(m_CurrentEntity))
            {
                m_CustomColorVariationSystem.CreateOrUpdateCustomColorVariationEntity(buffer, subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh, colorSet, m_CurrentAssetSeasonIdentifier.m_Index);
            }
            else
            {
                int length = Math.Min(4, subMeshBuffer.Length);
                for (int i = 0; i < length; i++)
                {
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase _))
                    {
                        continue;
                    }

                    m_CustomColorVariationSystem.CreateOrUpdateCustomColorVariationEntity(buffer, subMeshBuffer[i].m_SubMesh, colorSet, m_CurrentAssetSeasonIdentifier.m_Index);
                }
            }
        }

        private void DeleteCustomColorVariationEntity()
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
            if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) ||
                colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
            {
                return;
            }

            if (!EntityManager.HasComponent<Game.Objects.Tree>(m_CurrentEntity))
            {
                m_CustomColorVariationSystem.DeleteCustomColorVariationEntity(buffer, subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh);
            }
            else
            {
                int length = Math.Min(4, subMeshBuffer.Length);
                for (int i = 0; i < length; i++)
                {
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase _))
                    {
                        continue;
                    }

                    m_CustomColorVariationSystem.DeleteCustomColorVariationEntity(buffer, subMeshBuffer[i].m_SubMesh);
                }
            }
        }

        private void SaveColorSetToDisk()
        {
            if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
            {
                return;
            }

            ColorSet colorSet = colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index].m_ColorSet;
            if (!EntityManager.HasComponent<Game.Objects.Tree>(m_CurrentEntity))
            {
                TrySaveCustomColorSetToDisk(colorSet, m_CurrentAssetSeasonIdentifier);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase prefabBase))
                    {
                        continue;
                    }

                    AssetSeasonIdentifier assetSeasonIdentifier = new ()
                    {
                        m_Index = m_CurrentAssetSeasonIdentifier.m_Index,
                        m_PrefabID = prefabBase.GetPrefabID(),
                        m_Season = m_CurrentAssetSeasonIdentifier.m_Season,
                    };

                    TrySaveCustomColorSetToDisk(colorSet, assetSeasonIdentifier);
                }
            }

            m_State = State.UpdateButtonStates;

            EntityQuery prefabRefQuery = SystemAPI.QueryBuilder()
                .WithAll<PrefabRef>()
                .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                .Build();

            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();

            NativeArray<Entity> entities = prefabRefQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in entities)
            {
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) &&
                    EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer))
                {
                    for (int i = 0; i < currentSubMeshBuffer.Length; i++)
                    {
                        if (currentSubMeshBuffer[i].m_SubMesh == subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh)
                        {
                            buffer.AddComponent<BatchesUpdated>(e);
                            AddBatchesUpdatedToSubElements(e, buffer);
                            break;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Tries to save a custom color set.
        /// </summary>
        /// <param name="colorSet">Set of 3 colors.</param>
        /// <param name="assetSeasonIdentifier">struct with necessary data.</param>
        /// <returns>True if saved, false if error occured.</returns>
        private bool TrySaveCustomColorSetToDisk(ColorSet colorSet, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            string colorDataFilePath = GetAssetSeasonIdentifierFilePath(assetSeasonIdentifier);
            SavedColorSet customColorSet = new (colorSet, assetSeasonIdentifier);

            try
            {
                XmlSerializer serTool = new (typeof(SavedColorSet)); // Create serializer
                using (System.IO.FileStream file = System.IO.File.Create(colorDataFilePath)) // Create file
                {
                    serTool.Serialize(file, customColorSet); // Serialize whole properties
                }

                m_Log.Debug($"{nameof(SIPColorFieldsSystem)}.{nameof(TrySaveCustomColorSetToDisk)} saved color set for {assetSeasonIdentifier.m_PrefabID}.");
                return true;
            }
            catch (Exception ex)
            {
                m_Log.Warn($"{nameof(SIPColorFieldsSystem)}.{nameof(TrySaveCustomColorSetToDisk)} Could not save values for {assetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
                return false;
            }
        }

        private void RemoveFromDisk()
        {
            if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            if (!EntityManager.HasComponent<Game.Objects.Tree>(m_CurrentEntity))
            {
                TryDeleteSavedColorSetFile(m_CurrentAssetSeasonIdentifier);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase prefabBase))
                    {
                        continue;
                    }

                    AssetSeasonIdentifier assetSeasonIdentifier = new ()
                    {
                        m_Index = m_CurrentAssetSeasonIdentifier.m_Index,
                        m_PrefabID = prefabBase.GetPrefabID(),
                        m_Season = m_CurrentAssetSeasonIdentifier.m_Season,
                    };

                    TryDeleteSavedColorSetFile(assetSeasonIdentifier);
                }
            }

            m_State = State.UpdateButtonStates;
        }

        private void TryDeleteSavedColorSetFile(AssetSeasonIdentifier assetSeasonIdentifier)
        {
            string colorDataFilePath = GetAssetSeasonIdentifierFilePath(assetSeasonIdentifier);
            if (File.Exists(colorDataFilePath))
            {
                try
                {
                    System.IO.File.Delete(colorDataFilePath);
                }
                catch (Exception ex)
                {
                    m_Log.Warn($"{nameof(SIPColorFieldsSystem)}.{nameof(TryDeleteSavedColorSetFile)} Could not delete file for Set {assetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
                }
            }
            else
            {
                m_Log.Debug($"{nameof(SIPColorFieldsSystem)}.{nameof(TryDeleteSavedColorSetFile)} Could not find file for {assetSeasonIdentifier.m_PrefabID} {assetSeasonIdentifier.m_Season} {assetSeasonIdentifier.m_Index} at {GetAssetSeasonIdentifierFilePath(m_CurrentAssetSeasonIdentifier)}");
            }

        }

        /// <summary>
        /// Evaluates whether a color set for a prefab and season matches vanilla.
        /// </summary>
        /// <param name="colorSet">Comparison color set.</param>
        /// <param name="assetSeasonIdentifier">struct with necessary data.</param>
        /// <returns>True if match found. False if not.</returns>
        private bool MatchesSavedOnDiskColorSet(ColorSet colorSet, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            if (!TryLoadCustomColorSetFromDisk(assetSeasonIdentifier, out SavedColorSet customColorSet))
            {
                return false;
            }
            else
            {
                ColorSet savedColorSet = customColorSet.ColorSet;

                if (savedColorSet.m_Channel0 == colorSet.m_Channel0 &&
                    savedColorSet.m_Channel1 == colorSet.m_Channel1 &&
                    savedColorSet.m_Channel2 == colorSet.m_Channel2)
                {
                    return true;
                }
            }

            return false;
        }

        private bool[] MatchesVanillaColorSet(ColorSet colorSet, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            if (!m_VanillaColorSets.ContainsKey(assetSeasonIdentifier))
            {
                return new bool[] { false, false, false };
            }

            ColorSet vanillaColorSet = m_VanillaColorSets[assetSeasonIdentifier];
            bool[] matches = new bool[] { false, false, false };
            if (vanillaColorSet.m_Channel0 == colorSet.m_Channel0)
            {
                matches[0] = true;
            }

            if (vanillaColorSet.m_Channel1 == colorSet.m_Channel1)
            {
                matches[1] = true;
            }

            if (vanillaColorSet.m_Channel2 == colorSet.m_Channel2)
            {
                matches[2] = true;
            }

            return matches;
        }

        private bool TryLoadCustomColorSetFromDisk(AssetSeasonIdentifier assetSeasonIdentifier, out SavedColorSet result)
        {
            string colorDataFilePath = GetAssetSeasonIdentifierFilePath(assetSeasonIdentifier);
            result = default;
            if (File.Exists(colorDataFilePath))
            {
                try
                {
                    XmlSerializer serTool = new (typeof(SavedColorSet)); // Create serializer
                    using System.IO.FileStream readStream = new (colorDataFilePath, System.IO.FileMode.Open); // Open file
                    result = (SavedColorSet)serTool.Deserialize(readStream); // Des-serialize to new Properties

                    // m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TryLoadCustomColorSet)} loaded color set for {assetSeasonIdentifier.m_PrefabID}.");
                    return true;
                }
                catch (Exception ex)
                {
                    m_Log.Warn($"{nameof(SIPColorFieldsSystem)}.{nameof(TryLoadCustomColorSetFromDisk)} Could not get default values for Set {assetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets season from Season ID string.
        /// </summary>
        /// <param name="seasonID"> A string representing the season.</param>
        /// <returns>Season enum.</returns>
        private Season GetSeasonFromSeasonID(string seasonID)
        {
            if (SeasonDictionary.ContainsKey(seasonID))
            {
                return SeasonDictionary[seasonID];
            }

            Mod.Instance.Log.Info($"{nameof(SIPColorFieldsSystem)}.{nameof(GetSeasonFromSeasonID)} couldn't find season for {seasonID}.");
            return Season.None;
        }

        private string GetAssetSeasonIdentifierFilePath(AssetSeasonIdentifier assetSeasonIdentifier)
        {
            string prefabType = assetSeasonIdentifier.m_PrefabID.ToString().Remove(assetSeasonIdentifier.m_PrefabID.ToString().IndexOf(':'));
            return Path.Combine(m_ContentFolder, $"{prefabType}-{assetSeasonIdentifier.m_PrefabID.GetName()}-{assetSeasonIdentifier.m_Index}.xml");
        }

        private float CalculateCummulativeDifference(ColorSet actualColorSet, ColorSet colorVariation)
        {
            float difference = 0f;
            difference += Mathf.Abs(colorVariation.m_Channel0.r - actualColorSet.m_Channel0.r);
            difference += Mathf.Abs(colorVariation.m_Channel0.g - actualColorSet.m_Channel0.g);
            difference += Mathf.Abs(colorVariation.m_Channel0.b - actualColorSet.m_Channel0.b);
            difference += Mathf.Abs(colorVariation.m_Channel1.r - actualColorSet.m_Channel1.r);
            difference += Mathf.Abs(colorVariation.m_Channel1.g - actualColorSet.m_Channel1.g);
            difference += Mathf.Abs(colorVariation.m_Channel1.b - actualColorSet.m_Channel1.b);
            difference += Mathf.Abs(colorVariation.m_Channel2.r - actualColorSet.m_Channel2.r);
            difference += Mathf.Abs(colorVariation.m_Channel2.g - actualColorSet.m_Channel2.g);
            difference += Mathf.Abs(colorVariation.m_Channel2.b - actualColorSet.m_Channel2.b);
            return difference;
        }

        private void ReloadSavedColorSetsFromDisk()
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
                        using FileStream readStream = new (filePath, System.IO.FileMode.Open); // Open file
                        colorSet = (SavedColorSet)serTool.Deserialize(readStream);
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

                    AssetSeasonIdentifier assetSeasonIdentifier = new ()
                    {
                        m_PrefabID = prefabID,
                        m_Season = season,
                        m_Index = j,
                    };

                    if (TryLoadCustomColorSetFromDisk(assetSeasonIdentifier, out SavedColorSet customColorSet))
                    {
                        currentColorVariation.m_ColorSet = customColorSet.ColorSet;
                        colorVariationBuffer[j] = currentColorVariation;
                        if (!prefabsNeedingUpdates.Contains(e))
                        {
                            prefabsNeedingUpdates.Add(e);
                        }

                        m_Log.Debug($"{nameof(SIPColorFieldsSystem)}.{nameof(OnGameLoadingComplete)} Imported Colorset for {prefabID} in {assetSeasonIdentifier.m_Season}");
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
                    EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer))
                {
                    for (int i = 0; i < currentSubMeshBuffer.Length; i++)
                    {
                        if (prefabsNeedingUpdates.Contains(currentSubMeshBuffer[0].m_SubMesh))
                        {
                            buffer.AddComponent<BatchesUpdated>(e);
                            AddBatchesUpdatedToSubElements(e, buffer);
                            break;
                        }
                    }
                }
            }
        }

        private void ColorRefresh()
        {
            if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer) ||
                m_SubMeshData.Value.SubMeshIndex >= subMeshBuffer.Length)
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) ||
                 colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
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
                    EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer))
                {
                    for (int i = 0; i < currentSubMeshBuffer.Length; i++)
                    {
                        if (currentSubMeshBuffer[i].m_SubMesh == subMeshBuffer[m_SubMeshData.Value.SubMeshIndex].m_SubMesh)
                        {
                            buffer.AddComponent<BatchesUpdated>(e);
                            AddBatchesUpdatedToSubElements(e, buffer);
                            break;
                        }
                    }
                }
            }

            m_NeedsColorRefresh = false;
        }
    }
}
