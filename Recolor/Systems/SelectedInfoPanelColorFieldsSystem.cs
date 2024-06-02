﻿// <copyright file="SelectedInfoPanelColorFieldsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Recolor.Systems
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Extensions;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Entities.UniversalDelegates;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    /// Addes toggles to selected info panel for entites that can receive Anarchy mod components.
    /// </summary>
    public partial class SelectedInfoPanelColorFieldsSystem : ExtendedInfoSectionBase
    {
        /// <summary>
        ///  A way to lookup seasons.
        /// </summary>
        private readonly Dictionary<string, Season> SeasonDictionary = new ()
        {
                { "Climate.SEASON[Spring]", Season.Spring },
                { "Climate.SEASON[Summer]", Season.Summer },
                { "Climate.SEASON[Autumn]", Season.Autumn },
                { "Climate.SEASON[Winter]", Season.Winter },
        };

        private ILog m_Log;
        private ToolSystem m_ToolSystem;
        private Entity m_PreviouslySelectedEntity = Entity.Null;
        private ClimateSystem m_ClimateSystem;
        private ValueBindingHelper<RecolorSet> m_CurrentColorSet;
        private ValueBindingHelper<bool> m_MatchesSavedColorSet;
        private ValueBindingHelper<bool> m_SingleInstance;
        private ValueBindingHelper<bool> m_DisableSingleInstance;
        private ValueBindingHelper<bool> m_DisableMatching;
        private Dictionary<AssetSeasonIdentifier, Game.Rendering.ColorSet> m_VanillaColorSets;
        private EntityQuery m_SubMeshQuery;
        private ClimatePrefab m_ClimatePrefab;
        private AssetSeasonIdentifier m_CurrentAssetSeasonIdentifier;
        private string m_ContentFolder;
        private EndFrameBarrier m_Barrier;

        /// <summary>
        /// An enum to handle seasons.
        /// </summary>
        public enum Season
        {
            /// <summary>
            /// Does not have season.
            /// </summary>
            None = -1,

            /// <summary>
            /// Spring time.
            /// </summary>
            Spring,

            /// <summary>
            /// Summer time.
            /// </summary>
            Summer,

            /// <summary>
            /// Autumn or Fall.
            /// </summary>
            Autumn,

            /// <summary>
            /// Winter time.
            /// </summary>
            Winter,
        }

        /// <inheritdoc/>
        protected override string group => Mod.Id;

        /// <inheritdoc/>
        public override void OnWriteProperties(IJsonWriter writer)
        {
        }


        /// <inheritdoc/>
        protected override void OnProcess()
        {
        }

        /// <inheritdoc/>
        protected override void Reset()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_Log = Mod.Instance.Log;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Log.Info($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(OnCreate)}");
            m_ClimateSystem = World.GetOrCreateSystemManaged<ClimateSystem>();
            m_CurrentColorSet = CreateBinding("CurrentColorSet", new RecolorSet(default, default, default));
            m_MatchesSavedColorSet = CreateBinding("MatchesSavedColorSet", true);
            m_SingleInstance = CreateBinding("SingleInstance", true);
            m_DisableSingleInstance = CreateBinding("DisableSingleInstance", false);
            m_DisableMatching = CreateBinding("DisableMatching", false);
            CreateTrigger<int, UnityEngine.Color>("ChangeColor", ChangeColor);
            CreateTrigger("SaveColorSet", SaveColorSet);
            CreateTrigger("ResetColorSet", ResetColorSet);
            CreateTrigger("SingleInstance", () =>
            {
                m_SingleInstance.Value = true;
                m_PreviouslySelectedEntity = Entity.Null;
            });
            CreateTrigger("Matching", () =>
            {
                m_SingleInstance.Value = false;
                m_PreviouslySelectedEntity = Entity.Null;
            });

            m_VanillaColorSets = new ();
            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id, "SavedColorSet", "Custom");
            System.IO.Directory.CreateDirectory(m_ContentFolder);
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SubMeshQuery = SystemAPI.QueryBuilder()
                .WithAll<SubMesh>()
                .WithNone<PlaceholderObjectElement>()
                .Build();

            RequireForUpdate(m_SubMeshQuery);
            Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode.IsGame())
            {
                GetClimatePrefab();
                Enabled = true;
            }
            else
            {
                Enabled = false;
                return;
            }


            // Repalce this with loading each file in folder and implementing that.


            NativeList<Entity> colorVariationPrefabEntities = m_SubMeshQuery.ToEntityListAsync(Allocator.Temp, out JobHandle colorVariationPrefabJobHandle);
            colorVariationPrefabJobHandle.Complete();
            bool reloadPlantColors = false;

            foreach (Entity e in colorVariationPrefabEntities)
            {
                if (!EntityManager.TryGetBuffer(e, isReadOnly: false, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    continue;
                }

                for (int i = 0; i < Math.Min(4, subMeshBuffer.Length); i++)
                {
                    if (!EntityManager.TryGetBuffer(subMeshBuffer[i].m_SubMesh, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer))
                    {
                        continue;
                    }

                    PrefabBase prefabBase = m_PrefabSystem.GetPrefab<PrefabBase>(e);
                    PrefabID prefabID = prefabBase.GetPrefabID();

                    for (int j = 0; j < colorVariationBuffer.Length; j++)
                    {
#if VERBOSE
                    m_Log.Verbose($"{prefabID.GetName()} {(TreeState)(int)Math.Pow(2, i - 1)} {(FoliageUtils.Season)j} {colorVariationBuffer[j].m_ColorSet.m_Channel0} {colorVariationBuffer[j].m_ColorSet.m_Channel2} {colorVariationBuffer[j].m_ColorSet.m_Channel2}");
#endif
                        ColorVariation currentColorVariation = colorVariationBuffer[j];
                        if (!TryGetSeasonFromColorGroupID(currentColorVariation.m_GroupID, out Season season))
                        {
                            continue;
                        }

                        AssetSeasonIdentifier assetSeasonIdentifier = new ()
                        {
                            m_PrefabID = prefabID,
                            m_Season = season,
                            m_Index = j,
                        };

                        if (!m_VanillaColorSets.ContainsKey(assetSeasonIdentifier))
                        {
                            m_VanillaColorSets.Add(assetSeasonIdentifier, currentColorVariation.m_ColorSet);
                        }

                        if (TryLoadCustomColorSet(assetSeasonIdentifier, out SavedColorSet customColorSet))
                        {
                            currentColorVariation.m_ColorSet = customColorSet.ColorSet;
                            colorVariationBuffer[j] = currentColorVariation;
                            if (EntityManager.HasComponent<PlantData>(e))
                            {
                                reloadPlantColors = true;
                            }

                            m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(OnGameLoadingComplete)} Imported Colorset for {prefabID} in {assetSeasonIdentifier.m_Season}");
                        }
                    }
                }
            }

            if (reloadPlantColors)
            {
                EntityQuery plantQuery = SystemAPI.QueryBuilder()
                   .WithAll<Game.Objects.Plant>()
                   .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                   .Build();

                NativeArray<Entity> entities = plantQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity e in entities)
                {
                    EntityManager.AddComponent<BatchesUpdated>(e);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (selectedEntity == Entity.Null && m_PreviouslySelectedEntity != Entity.Null)
            {
                m_PreviouslySelectedEntity = Entity.Null;
            }

            if (selectedEntity == Entity.Null)
            {
                visible = false;
                return;
            }

            if (m_PreviouslySelectedEntity == Entity.Null || m_PreviouslySelectedEntity != selectedEntity)
            {
                visible = false;
            }

            bool foundClimatePrefab = true;
            if (m_ClimatePrefab is null)
            {
                foundClimatePrefab = GetClimatePrefab();
            }

            if (EntityManager.HasComponent<Game.Objects.Plant>(selectedEntity) && !m_DisableSingleInstance)
            {
                m_DisableSingleInstance.Value = true;
            }
            else if (m_DisableSingleInstance.Value)
            {
                m_DisableSingleInstance.Value = false;
            }

            if (EntityManager.HasBuffer<CustomMeshColor>(selectedEntity) && !m_DisableMatching)
            {
                m_DisableMatching.Value = true;
            }
            else if (m_DisableMatching.Value)
            {
                m_DisableMatching.Value = false;
            }

            if (!EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                return;
            }

            ColorSet originalMeshColor = meshColorBuffer[0].m_ColorSet;
            if (EntityManager.TryGetComponent(selectedEntity, out Game.Objects.Tree tree))
            {
                if (tree.m_State == Game.Objects.TreeState.Dead || tree.m_State == Game.Objects.TreeState.Collected || tree.m_State == Game.Objects.TreeState.Stump)
                {
                    visible = false;
                    return;
                }

                if ((int)tree.m_State <= 1)
                {
                    originalMeshColor = meshColorBuffer[(int)tree.m_State].m_ColorSet;
                }
                else
                {
                    originalMeshColor = meshColorBuffer[(int)Math.Log((int)tree.m_State, 2)].m_ColorSet;
                }
            }

            // Colors Variation
            if (m_PreviouslySelectedEntity != selectedEntity
                && selectedEntity != Entity.Null
                && selectedPrefab != Entity.Null
                && (!m_SingleInstance.Value || EntityManager.HasComponent<Game.Objects.Plant>(selectedEntity))
                && !EntityManager.HasBuffer<CustomMeshColor>(selectedEntity)
                && foundClimatePrefab
                && m_PrefabSystem.TryGetPrefab(selectedPrefab, out PrefabBase prefabBase)
                && EntityManager.TryGetBuffer(selectedPrefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer)
                && EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer)
                && colorVariationBuffer.Length > 0)
            {
                Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);

                ColorSet colorSet = colorVariationBuffer[0].m_ColorSet;
                int index = 0;
                float cummulativeDifference = float.MaxValue;
                for (int i = 0; i < colorVariationBuffer.Length; i++)
                {
                    if ((TryGetSeasonFromColorGroupID(colorVariationBuffer[i].m_GroupID, out Season season) && season == currentSeason) || season == Season.None)
                    {
                        float currentCummulativeDifference = CalculateCummulativeDifference(originalMeshColor, colorVariationBuffer[i].m_ColorSet);
                        if (currentCummulativeDifference < cummulativeDifference)
                        {
                            cummulativeDifference = currentCummulativeDifference;
                            index = i;
                            colorSet = colorVariationBuffer[i].m_ColorSet;
                        }
                    }
                }

                visible = true;
                m_CurrentAssetSeasonIdentifier = new AssetSeasonIdentifier()
                {
                    m_Index = index,
                    m_PrefabID = prefabBase.GetPrefabID(),
                    m_Season = currentSeason,
                };

                m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(OnUpdate)} m_CurrentAssetSeasonIdentifier.m_Index {m_CurrentAssetSeasonIdentifier.m_Index}");
                m_CurrentColorSet.Value = new RecolorSet(colorSet);

                m_PreviouslySelectedEntity = selectedEntity;

                m_MatchesSavedColorSet.Value = MatchesSavedColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
            }

            if (m_PreviouslySelectedEntity != selectedEntity
                && m_SingleInstance
                && meshColorBuffer.Length > 0)
            {
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[0].m_ColorSet);

                m_PreviouslySelectedEntity = selectedEntity;

                visible = true;
            }
        }

        private bool GetClimatePrefab()
        {
            Entity currentClimate = m_ClimateSystem.currentClimate;
            if (currentClimate == Entity.Null)
            {
                m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(GetClimatePrefab)} couldn't find climate entity.");
                return false;
            }

            if (!m_PrefabSystem.TryGetPrefab(m_ClimateSystem.currentClimate, out m_ClimatePrefab))
            {
                m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(GetClimatePrefab)} couldn't find climate prefab.");
                return false;
            }

            return true;
        }

        private void ChangeColor(int channel, UnityEngine.Color color)
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
            if (m_SingleInstance && EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                if (!EntityManager.HasBuffer<CustomMeshColor>(selectedEntity))
                {
                    DynamicBuffer<CustomMeshColor> newBuffer = EntityManager.AddBuffer<CustomMeshColor>(selectedEntity);
                    foreach (MeshColor meshColor in meshColorBuffer)
                    {
                        newBuffer.Add(new CustomMeshColor(meshColor));
                    }
                }

                if (!EntityManager.TryGetBuffer(selectedEntity, isReadOnly: false, out DynamicBuffer<CustomMeshColor> customMeshColorBuffer))
                {
                    return;
                }

                for (int i = 0; i < Math.Min(4, meshColorBuffer.Length); i++)
                {
                    CustomMeshColor customMeshColor = customMeshColorBuffer[i];
                    if (channel == 0)
                    {
                        customMeshColor.m_ColorSet.m_Channel0 = color;
                    }
                    else if (channel == 1)
                    {
                        customMeshColor.m_ColorSet.m_Channel2 = color;
                    }
                    else if (channel == 2)
                    {
                        customMeshColor.m_ColorSet.m_Channel2 = color;
                    }

                    customMeshColorBuffer[i] = customMeshColor;
                }
            }
            else
            {
                if (selectedPrefab == Entity.Null || !EntityManager.TryGetBuffer(selectedPrefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    return;
                }


                for (int i = 0; i < Math.Min(4, subMeshBuffer.Length); i++)
                {
                    if (!EntityManager.TryGetBuffer(subMeshBuffer[i].m_SubMesh, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
                    {
                        continue;
                    }

                    ColorVariation colorVariation = colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index];

                    if (channel == 0)
                    {
                        colorVariation.m_ColorSet.m_Channel0 = color;
                    }
                    else if (channel == 1)
                    {
                        colorVariation.m_ColorSet.m_Channel2 = color;
                    }
                    else if (channel == 2)
                    {
                        colorVariation.m_ColorSet.m_Channel2 = color;
                    }

                    colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index] = colorVariation;
                }
            }

            m_PreviouslySelectedEntity = Entity.Null;
            buffer.AddComponent<BatchesUpdated>(selectedEntity);
        }

        private void SaveColorSet()
        {
            if (!EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef))
            {
                return;
            }

            if (!m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabBase))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < m_CurrentAssetSeasonIdentifier.m_Index)
            {
                return;
            }

            ColorSet colorSet = colorVariationBuffer[m_CurrentAssetSeasonIdentifier.m_Index].m_ColorSet;

            TrySaveCustomColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
            m_PreviouslySelectedEntity = Entity.Null;

            if (EntityManager.HasComponent<Game.Objects.Plant>(selectedEntity))
            {
                EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
                EntityQuery plantQuery = SystemAPI.QueryBuilder()
                   .WithAll<Game.Objects.Plant>()
                   .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                   .Build();

                NativeArray<Entity> entities = plantQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity e in entities)
                {
                    if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && currentPrefabRef.m_Prefab == prefabRef.m_Prefab)
                    {
                        buffer.AddComponent<BatchesUpdated>(e);
                    }
                }
            }

        }


        private void ResetColorSet()
        {
            if (!EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef))
            {
                return;
            }

            if (!TryGetVanillaColorSet(m_CurrentAssetSeasonIdentifier, out ColorSet vanillaColorSet))
            {
                return;
            }

            ChangeColor(0, vanillaColorSet.m_Channel0);
            ChangeColor(1, vanillaColorSet.m_Channel2);
            ChangeColor(2, vanillaColorSet.m_Channel2);
            m_PreviouslySelectedEntity = Entity.Null;

            string colorDataFilePath = GetAssetSeasonIdentifierFilePath(m_CurrentAssetSeasonIdentifier);
            if (File.Exists(colorDataFilePath))
            {
                try
                {
                    System.IO.File.Delete(colorDataFilePath);
                }
                catch (Exception ex)
                {
                    m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(ResetColorSet)} Could not get default values for Set {m_CurrentAssetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
                }
            }

            if (EntityManager.HasComponent<Game.Objects.Plant>(selectedEntity))
            {
                EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();
                EntityQuery plantQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Objects.Plant>()
                   .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                   .Build();

                NativeArray<Entity> entities = plantQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity e in entities)
                {
                    if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && currentPrefabRef.m_Prefab == prefabRef.m_Prefab)
                    {
                        buffer.AddComponent<BatchesUpdated>(e);
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
        private bool TrySaveCustomColorSet(ColorSet colorSet, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            string colorDataFilePath = GetAssetSeasonIdentifierFilePath(assetSeasonIdentifier);
            SavedColorSet customColorSet = new SavedColorSet(colorSet, assetSeasonIdentifier);

            try
            {
                XmlSerializer serTool = new XmlSerializer(typeof(SavedColorSet)); // Create serializer
                using (System.IO.FileStream file = System.IO.File.Create(colorDataFilePath)) // Create file
                {
                    serTool.Serialize(file, customColorSet); // Serialize whole properties
                }

                m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TrySaveCustomColorSet)} saved color set for {assetSeasonIdentifier.m_PrefabID}.");
                return true;
            }
            catch (Exception ex)
            {
                m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TrySaveCustomColorSet)} Could not save values for {assetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
                return false;
            }
        }

        /// <summary>
        /// Evaluates whether a color set for a prefab and season matches vanilla.
        /// </summary>
        /// <param name="colorSet">Comparison color set.</param>
        /// <param name="assetSeasonIdentifier">struct with necessary data.</param>
        /// <returns>True if match found. False if not.</returns>
        private bool MatchesSavedColorSet(ColorSet colorSet, AssetSeasonIdentifier assetSeasonIdentifier)
        {
            bool foundVanilla = m_VanillaColorSets.ContainsKey(assetSeasonIdentifier);

            if (!TryLoadCustomColorSet(assetSeasonIdentifier, out SavedColorSet customColorSet))
            {
                if (!foundVanilla)
                {
                    return false;
                }

                ColorSet vanillaColorSet = m_VanillaColorSets[assetSeasonIdentifier];

                if (vanillaColorSet.m_Channel0 == colorSet.m_Channel0 && vanillaColorSet.m_Channel2 == colorSet.m_Channel2 && vanillaColorSet.m_Channel2 == colorSet.m_Channel2)
                {
                    return true;
                }
            }
            else
            {
                ColorSet savedColorSet = customColorSet.ColorSet;

                if (savedColorSet.m_Channel0 == colorSet.m_Channel0 && savedColorSet.m_Channel2 == colorSet.m_Channel2 && savedColorSet.m_Channel2 == colorSet.m_Channel2)
                {
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
        private bool TryGetVanillaColorSet(AssetSeasonIdentifier assetSeasonIdentifier, out ColorSet colorSet)
        {
            colorSet = default;
            if (!m_VanillaColorSets.ContainsKey(assetSeasonIdentifier))
            {
                return false;
            }

            colorSet = m_VanillaColorSets[assetSeasonIdentifier];
            return true;
        }

        private bool TryLoadCustomColorSet(AssetSeasonIdentifier assetSeasonIdentifier, out SavedColorSet result)
        {
            string colorDataFilePath = GetAssetSeasonIdentifierFilePath(assetSeasonIdentifier);
            result = default;
            if (File.Exists(colorDataFilePath))
            {
                try
                {
                    XmlSerializer serTool = new XmlSerializer(typeof(SavedColorSet)); // Create serializer
                    using System.IO.FileStream readStream = new System.IO.FileStream(colorDataFilePath, System.IO.FileMode.Open); // Open file
                    result = (SavedColorSet)serTool.Deserialize(readStream); // Des-serialize to new Properties


                    m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TryLoadCustomColorSet)} loaded color set for {assetSeasonIdentifier.m_PrefabID}.");
                    return true;
                }
                catch (Exception ex)
                {
                    m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TryLoadCustomColorSet)} Could not get default values for Set {assetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
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

            Mod.Instance.Log.Info($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(GetSeasonFromSeasonID)} couldn't find season for {seasonID}.");
            return Season.Spring;
        }

        /// <summary>
        /// Gets season from color group id using reflection.
        /// </summary>
        /// <param name="colorGroupID">Color group ID from color variation</param>
        /// <param name="season">outputted season or spring if false.</param>
        /// <returns>true is converted, false if not.</returns>
        private bool TryGetSeasonFromColorGroupID(ColorGroupID colorGroupID, out Season season)
        {
            var index = colorGroupID.GetMemberValue("m_Index");
            season = Season.None;
            if (index is int && (int)index > 0 && (int)index < 4)
            {
                season = (Season)index;
                return true;
            }

            return false;
        }

        private string GetAssetSeasonIdentifierFilePath(AssetSeasonIdentifier assetSeasonIdentifier)
        {
            string prefabType = assetSeasonIdentifier.m_PrefabID.ToString().Remove(assetSeasonIdentifier.m_PrefabID.ToString().IndexOf(':'));
            return Path.Combine(m_ContentFolder, $"{prefabType}-{assetSeasonIdentifier.m_PrefabID.GetName()}-{assetSeasonIdentifier.m_Season}-{assetSeasonIdentifier.m_Index}.xml");
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

        public struct AssetSeasonIdentifier
        {
            public PrefabID m_PrefabID;
            public Season m_Season;
            public int m_Index;
        }
    }
}
