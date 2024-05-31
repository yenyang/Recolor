// <copyright file="SelectedInfoPanelColorFieldsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Recolor.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using Recolor.Extensions;
    using Unity.Collections;
    using Unity.Entities;
    using Colossal.PSI.Environment;
    using Recolor.Domain;
    using Unity.Jobs;
    using static Game.Prefabs.TriggerPrefabData;
    using static Recolor.Systems.SelectedInfoPanelColorFieldsSystem;

    /// <summary>
    /// Addes toggles to selected info panel for entites that can receive Anarchy mod components.
    /// </summary>
    public partial class SelectedInfoPanelColorFieldsSystem : ExtendedInfoSectionBase
    {
        /// <summary>
        ///  A way to lookup seasons.
        /// </summary>
        private readonly Dictionary<string, Season> SeasonDictionary = new()
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
        private Dictionary<AssetSeasonIdentifier, Game.Rendering.ColorSet> m_VanillaColorSets;
        private EntityQuery m_SubMeshQuery;
        private ClimatePrefab m_ClimatePrefab;
        private AssetSeasonIdentifier m_CurrentAssetSeasonIdentifier;
        private string m_ContentFolder;

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
            CreateTrigger<int, UnityEngine.Color>("ChangeColor", ChangeColor);
            CreateTrigger("SaveColorSet", SaveColorSet);
            CreateTrigger("ResetColorSet", ResetColorSet);
            m_VanillaColorSets = new();
            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id, "SavedColorSet", "Custom");
            System.IO.Directory.CreateDirectory(m_ContentFolder);

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
                    m_Log.Verbose($"{prefabID.GetName()} {(TreeState)(int)Math.Pow(2, i - 1)} {(FoliageUtils.Season)j} {colorVariationBuffer[j].m_ColorSet.m_Channel0} {colorVariationBuffer[j].m_ColorSet.m_Channel1} {colorVariationBuffer[j].m_ColorSet.m_Channel2}");
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

            if (m_PreviouslySelectedEntity == Entity.Null || m_PreviouslySelectedEntity != selectedEntity)
            {
                visible = false;
            }

            if (m_ClimatePrefab is null)
            {
                if (GetClimatePrefab() == false)
                {
                    visible = false;
                    return;
                }
            }

            if (m_PreviouslySelectedEntity != selectedEntity && selectedEntity != Entity.Null)
            {
                if (!EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef))
                {
                    return;
                }

                if (!EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    return;
                }

                Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);

                if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer))
                {
                    return;
                }

                bool hasSeasonalColors = false;
                ColorSet colorSet = colorVariationBuffer[0].m_ColorSet;
                int index = 0;
                for (int i = 0; i < colorVariationBuffer.Length; i++)
                {
                    if (TryGetSeasonFromColorGroupID(colorVariationBuffer[i].m_GroupID, out Season season) && season == currentSeason)
                    {
                        colorSet = colorVariationBuffer[i].m_ColorSet;
                        index = i;
                        hasSeasonalColors = true;
                        break;
                    }
                }

                if (!hasSeasonalColors)
                {
                    visible = false;
                    return;
                }

                if (!m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out PrefabBase prefabBase))
                {
                    return;
                }


                visible = true;

                m_CurrentAssetSeasonIdentifier = new AssetSeasonIdentifier()
                {
                    m_Index = index,
                    m_PrefabID = prefabBase.GetPrefabID(),
                    m_Season = currentSeason,
                };

                m_CurrentColorSet.Value = new RecolorSet(colorSet);

                m_PreviouslySelectedEntity = selectedEntity;

                m_MatchesSavedColorSet.Value = MatchesSavedColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
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
            if (!EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef))
            {
                return;
            }

            if (!EntityManager.TryGetBuffer(prefabRef.m_Prefab, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
            {
                return;
            }

            Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);

            for (int i = 0; i < Math.Min(4, subMeshBuffer.Length); i++)
            {
                if (!EntityManager.TryGetBuffer(subMeshBuffer[i].m_SubMesh, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < 4)
                {
                    continue;
                }

                ColorVariation colorVariation = colorVariationBuffer[(int)currentSeason];

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

                colorVariationBuffer[(int)currentSeason] = colorVariation;
            }

            m_Log.Debug($"Changed Color {channel} {color}");
            m_PreviouslySelectedEntity = Entity.Null;
            EntityManager.AddComponent<BatchesUpdated>(selectedEntity);
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

            Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);

            if (!EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer) || colorVariationBuffer.Length < 4)
            {
                return;
            }

            ColorSet colorSet = colorVariationBuffer[(int)currentSeason].m_ColorSet;

            TrySaveCustomColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
            m_PreviouslySelectedEntity = Entity.Null;

            if (EntityManager.HasComponent<Game.Objects.Plant>(selectedEntity))
            {
                EntityQuery plantQuery = SystemAPI.QueryBuilder()
                   .WithAll<Game.Objects.Plant>()
                   .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                   .Build();

                NativeArray<Entity> entities = plantQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity e in entities)
                {
                    if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && currentPrefabRef.m_Prefab == prefabRef.m_Prefab)
                    {
                        EntityManager.AddComponent<BatchesUpdated>(e);
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
            ChangeColor(1, vanillaColorSet.m_Channel1);
            ChangeColor(2, vanillaColorSet.m_Channel2);
            m_PreviouslySelectedEntity = Entity.Null;

            string colorDataFilePath = Path.Combine(m_ContentFolder, $"{m_CurrentAssetSeasonIdentifier.m_PrefabID.GetName()}-{m_CurrentAssetSeasonIdentifier.m_Season}-{m_CurrentAssetSeasonIdentifier.m_Index}.xml");
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
                EntityQuery plantQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Objects.Plant>()
                   .WithNone<Deleted, Game.Common.Overridden, Game.Tools.Temp>()
                   .Build();

                NativeArray<Entity> entities = plantQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity e in entities)
                {
                    if (EntityManager.TryGetComponent(e, out PrefabRef currentPrefabRef) && currentPrefabRef.m_Prefab == prefabRef.m_Prefab)
                    {
                        EntityManager.AddComponent<BatchesUpdated>(e);
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
            string colorDataFilePath = Path.Combine(m_ContentFolder, $"{assetSeasonIdentifier.m_PrefabID.GetName()}-{assetSeasonIdentifier.m_Season}-{assetSeasonIdentifier.m_Index}.xml");
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

                if (vanillaColorSet.m_Channel0 == colorSet.m_Channel0 && vanillaColorSet.m_Channel1 == colorSet.m_Channel1 && vanillaColorSet.m_Channel2 == colorSet.m_Channel2)
                {
                    return true;
                }
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
            string colorDataFilePath = Path.Combine(m_ContentFolder, $"{assetSeasonIdentifier.m_PrefabID.GetName()}-{assetSeasonIdentifier.m_Season}-{assetSeasonIdentifier.m_Index}.xml");
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
        
        public struct AssetSeasonIdentifier
        {
            public PrefabID m_PrefabID;
            public Season m_Season;
            public int m_Index;
        }

    }
}
