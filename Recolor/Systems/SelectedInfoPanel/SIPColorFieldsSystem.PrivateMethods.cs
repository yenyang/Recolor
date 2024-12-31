// <copyright file="SIPColorFieldsSystem.PrivateMethods.cs" company="Yenyang's Mods. MIT License">
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
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;
    using static Game.Rendering.OverlayRenderSystem;

    /// <summary>
    ///  Adds color fields to selected info panel for changing colors of buildings, vehicles, props, etc.
    /// </summary>
    public partial class SIPColorFieldsSystem : ExtendedInfoSectionBase
    {
        private bool GetClimatePrefab()
        {
            Entity currentClimate = m_ClimateSystem.currentClimate;
            if (currentClimate == Entity.Null)
            {
                m_Log.Warn($"{nameof(SIPColorFieldsSystem)}.{nameof(GetClimatePrefab)} couldn't find climate entity.");
                return false;
            }

            if (!m_PrefabSystem.TryGetPrefab(m_ClimateSystem.currentClimate, out m_ClimatePrefab))
            {
                m_Log.Warn($"{nameof(SIPColorFieldsSystem)}.{nameof(GetClimatePrefab)} couldn't find climate prefab.");
                return false;
            }

            return true;
        }

        private void CopyColor(UnityEngine.Color color)
        {
            m_CanPasteColor.Value = true;
            m_CopiedColor = color;
        }

        private void CopyColorSet()
        {
            m_CanPasteColorSet.Value = true;
            m_CopiedColorSet = m_CurrentColorSet.Value.GetColorSet();
        }

        private void PasteColor(int channel)
        {
            ChangeColor(channel, m_CopiedColor);
        }

        private void PasteColorSet()
        {
            ChangeColor(0, m_CopiedColorSet.m_Channel0);
            ChangeColor(1, m_CopiedColorSet.m_Channel1);
            ChangeColor(2, m_CopiedColorSet.m_Channel2);
        }

        private void ChangeColorAction(int channel, UnityEngine.Color color)
        {
            ChangeColor(channel, color);
        }

        private ColorSet ChangeColor(int channel, UnityEngine.Color color)
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
            if ((m_SingleInstance.Value || m_DisableMatching.Value) && !m_DisableSingleInstance && !EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity) && EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                if (!EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity))
                {
                    DynamicBuffer<CustomMeshColor> newBuffer = EntityManager.AddBuffer<CustomMeshColor>(m_CurrentEntity);
                    foreach (MeshColor meshColor in meshColorBuffer)
                    {
                        newBuffer.Add(new CustomMeshColor(meshColor));
                    }
                }

                if (!EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: false, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer))
                {
                    return default;
                }

                ColorSet colorSet = default;
                int length = meshColorBuffer.Length;
                if (EntityManager.HasComponent<Tree>(m_CurrentEntity))
                {
                    length = Math.Min(4, meshColorBuffer.Length);
                }

                for (int i = 0; i < length; i++)
                {
                    CustomMeshColor customMeshColor = customMeshColorBuffer[i];
                    if (channel == 0)
                    {
                        customMeshColor.m_ColorSet.m_Channel0 = color;
                    }
                    else if (channel == 1)
                    {
                        customMeshColor.m_ColorSet.m_Channel1 = color;
                    }
                    else if (channel == 2)
                    {
                        customMeshColor.m_ColorSet.m_Channel2 = color;
                    }

                    customMeshColorBuffer[i] = customMeshColor;
                    m_TimeColorLastChanged = UnityEngine.Time.time;
                    m_PreviouslySelectedEntity = Entity.Null;
                    buffer.AddComponent<BatchesUpdated>(m_CurrentEntity);
                    AddBatchesUpdatedToSubElements(m_CurrentEntity, buffer);
                    colorSet = customMeshColor.m_ColorSet;
                }

                return colorSet;
            }
            else if (!m_DisableMatching && !EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity))
            {
                if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    return default;
                }

                ColorVariation colorVariation = default;
                int length = subMeshBuffer.Length;
                if (EntityManager.HasComponent<Tree>(m_CurrentEntity))
                {
                    length = Math.Min(4, subMeshBuffer.Length);
                }

                for (int i = 0; i < length; i++)
                {
                    if (!EntityManager.TryGetBuffer(subMeshBuffer[i].m_SubMesh, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
                    {
                        continue;
                    }

                    colorVariation = colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index];

                    if (channel == 0)
                    {
                        colorVariation.m_ColorSet.m_Channel0 = color;
                    }
                    else if (channel == 1)
                    {
                        colorVariation.m_ColorSet.m_Channel1 = color;
                    }
                    else if (channel == 2)
                    {
                        colorVariation.m_ColorSet.m_Channel2 = color;
                    }

                    colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index] = colorVariation;
                    m_TimeColorLastChanged = UnityEngine.Time.time;
                    m_PreviouslySelectedEntity = Entity.Null;
                    buffer.AddComponent<BatchesUpdated>(m_CurrentEntity);
                    AddBatchesUpdatedToSubElements(m_CurrentEntity, buffer);
                }

                GenerateOrUpdateCustomColorVariationEntity();
                m_NeedsColorRefresh = true;

                return colorVariation.m_ColorSet;
            }

            return default;
        }

        private void GenerateOrUpdateCustomColorVariationEntity()
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
            if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
            {
                return;
            }

            ColorSet colorSet = colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index].m_ColorSet;
            if (!EntityManager.HasComponent<Game.Objects.Tree>(m_CurrentEntity))
            {
                m_CustomColorVariationSystem.CreateOrUpdateCustomColorVariationEntity(buffer, subMeshBuffer[0].m_SubMesh, colorSet, m_CurrentAssetSeasonIdentifier.m_Index);
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

            if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
            {
                return;
            }

            if (!EntityManager.HasComponent<Game.Objects.Tree>(m_CurrentEntity))
            {
                m_CustomColorVariationSystem.DeleteCustomColorVariationEntity(buffer, subMeshBuffer[0].m_SubMesh);
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

            if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
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

            m_PreviouslySelectedEntity = Entity.Null;

            EntityQuery prefabRefQuery = SystemAPI.QueryBuilder()
                .WithAll<PrefabRef>()
                .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                .Build();

            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();

            NativeArray<Entity> entities = prefabRefQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity e in entities)
            {
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer) && currentSubMeshBuffer[0].m_SubMesh == subMeshBuffer[0].m_SubMesh)
                {
                    buffer.AddComponent<BatchesUpdated>(e);
                    AddBatchesUpdatedToSubElements(e, buffer);
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

        private void ResetColorSet()
        {
            ResetColor(0);
            ResetColor(1);
            ResetColor(2);
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

            m_PreviouslySelectedEntity = Entity.Null;
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

        private void ResetColor(int channel)
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();

            if (EntityManager.HasComponent<CustomMeshColor>(m_CurrentEntity))
            {
                buffer.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                buffer.AddComponent<BatchesUpdated>(m_CurrentEntity);

                AddBatchesUpdatedToSubElements(m_CurrentEntity, buffer);

                m_PreviouslySelectedEntity = Entity.Null;
                return;
            }

            if (!TryGetVanillaColorSet(m_CurrentAssetSeasonIdentifier, out ColorSet vanillaColorSet))
            {
                m_Log.Info($"{nameof(SIPColorFieldsSystem)}.{nameof(ResetColor)} Could not find vanilla color set for {m_CurrentAssetSeasonIdentifier.m_PrefabID} {m_CurrentAssetSeasonIdentifier.m_Season} {m_CurrentAssetSeasonIdentifier.m_Index}");
                return;
            }

            ColorSet colorSet = default;
            if (channel == 0)
            {
                colorSet = ChangeColor(0, vanillaColorSet.m_Channel0);
            }
            else if (channel == 1)
            {
                colorSet = ChangeColor(1, vanillaColorSet.m_Channel1);
            }
            else if (channel == 2)
            {
                colorSet = ChangeColor(2, vanillaColorSet.m_Channel2);
            }

            bool[] matches = MatchesVanillaColorSet(colorSet, m_CurrentAssetSeasonIdentifier);

            m_PreviouslySelectedEntity = Entity.Null;

            if (matches.Length >= 3 && matches[0] && matches[1] && matches[2])
            {
                ColorRefresh();
            }
            else
            {
                GenerateOrUpdateCustomColorVariationEntity();
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

                if (savedColorSet.m_Channel0 == colorSet.m_Channel0 && savedColorSet.m_Channel1 == colorSet.m_Channel1 && savedColorSet.m_Channel2 == colorSet.m_Channel2)
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

        private void ColorRefresh()
        {
            if (!EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
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
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer) && currentSubMeshBuffer[0].m_SubMesh == subMeshBuffer[0].m_SubMesh)
                {
                    buffer.AddComponent<BatchesUpdated>(e);

                    AddBatchesUpdatedToSubElements(e, buffer);
                }
            }

            m_NeedsColorRefresh = false;
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
                if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && EntityManager.TryGetBuffer(currentPrefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> currentSubMeshBuffer) && prefabsNeedingUpdates.Contains(currentSubMeshBuffer[0].m_SubMesh))
                {
                    buffer.AddComponent<BatchesUpdated>(e);

                    AddBatchesUpdatedToSubElements(e, buffer);
                }
            }
        }
    }
}
