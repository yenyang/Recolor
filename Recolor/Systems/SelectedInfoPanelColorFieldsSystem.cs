// <copyright file="SelectedInfoPanelColorFieldsSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
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
    using Unity.Collections;
    using Unity.Entities;
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
        private ValueBindingHelper<bool[]> m_MatchesVanillaColorSet;
        private ValueBindingHelper<bool> m_SingleInstance;
        private ValueBindingHelper<bool> m_DisableSingleInstance;
        private ValueBindingHelper<bool> m_DisableMatching;
        private ValueBindingHelper<bool> m_CanPasteColor;
        private ValueBindingHelper<bool> m_CanPasteColorSet;
        private ValueBindingHelper<bool> m_Minimized;
        private Dictionary<AssetSeasonIdentifier, Game.Rendering.ColorSet> m_VanillaColorSets;
        private ValueBindingHelper<bool> m_MatchesSavedOnDisk;
        private ColorPickerToolSystem m_ColorPickerTool;
        private ColorPainterToolSystem m_ColorPainterTool;
        private ColorPainterUISystem m_ColorPainterUISystem;
        private CustomColorVariationSystem m_CustomColorVariationSystem;
        private EntityQuery m_SubMeshQuery;
        private ProxyAction m_ActivateColorPainterAction;
        private ClimatePrefab m_ClimatePrefab;
        private AssetSeasonIdentifier m_CurrentAssetSeasonIdentifier;
        private string m_ContentFolder;
        private int m_ReloadInXFrames = 0;
        private float m_TimeColorLastChanged = 0f;
        private bool m_NeedsColorRefresh = false;
        private UnityEngine.Color m_CopiedColor;
        private ColorSet m_CopiedColorSet;
        private bool m_ActivateColorPainter;
        private Entity m_CurrentEntity;
        private Entity m_CurrentPrefabEntity;

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

        /// <summary>
        /// Gets a value indicating whether to Disable Matching.
        /// </summary>
        public bool DisableMatching
        {
            get { return m_DisableMatching.Value; }
        }

        /// <summary>
        /// Gets a value indicating whether to change single instance or not.
        /// </summary>
        public bool SingleInstance
        {
            get { return m_SingleInstance.Value; }
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
        /// Gets a the copied color set.
        /// </summary>
        public UnityEngine.Color CopiedColor
        {
            get { return m_CopiedColor; }
        }

        /// <inheritdoc/>
        protected override string group => Mod.Id;

        /// <inheritdoc/>
        protected override bool displayForUpgrades => true;

        /// <summary>
        /// Resets the previously selected entity.
        /// </summary>
        public void ResetPreviouslySelectedEntity()
        {
            m_PreviouslySelectedEntity = Entity.Null;
        }

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
            for (int i = 0; i <= 3; i++)
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

            Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);

            colorSet = colorVariationBuffer[0].m_ColorSet;
            int index = 0;
            float cummulativeDifference = float.MaxValue;
            Season season = Season.None;
            for (int i = 0; i < colorVariationBuffer.Length; i++)
            {
                if ((TryGetSeasonFromColorGroupID(colorVariationBuffer[i].m_GroupID, out Season checkSeason) && checkSeason == currentSeason) || checkSeason == Season.None)
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
            NativeList<Entity> prefabsNeedingUpdates = new NativeList<Entity>(Allocator.Temp);
            foreach (string filePath in filePaths)
            {
                SavedColorSet colorSet = default;
                if (File.Exists(filePath))
                {
                    try
                    {
                        XmlSerializer serTool = new XmlSerializer(typeof(SavedColorSet)); // Create serializer
                        using (System.IO.FileStream readStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open)) // Open
                        {
                            colorSet = (SavedColorSet)serTool.Deserialize(readStream);
                        }

                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(ReloadSavedColorSetsFromDisk)} Could not deserialize file at {filePath}. Encountered exception {ex}");
                        continue;
                    }
                }

                PrefabID prefabID = new PrefabID(colorSet.PrefabType, colorSet.PrefabName);

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

                    if (!m_CustomColorVariationSystem.TryGetCustomColorVariation(e, j, out CustomColorVariations customColorVariation) && TryGetVanillaColorSet(assetSeasonIdentifier, out currentColorVariation.m_ColorSet))
                    {
                        colorVariationBuffer[j] = currentColorVariation;
                        if (!prefabsNeedingUpdates.Contains(e))
                        {
                            prefabsNeedingUpdates.Add(e);
                        }

                        m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(DeleteAllModsDataFiles)} Reset Colorset for {prefabID} in {assetSeasonIdentifier.m_Season}");
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
                }
            }
        }

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
            m_ColorPainterTool = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_ColorPainterUISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_ColorPickerTool = World.GetOrCreateSystemManaged<ColorPickerToolSystem>();
            m_CustomColorVariationSystem = World.GetOrCreateSystemManaged<CustomColorVariationSystem>();

            // These establish bindings to communicate between UI and C#.
            m_CurrentColorSet = CreateBinding("CurrentColorSet", new RecolorSet(default, default, default));
            m_MatchesVanillaColorSet = CreateBinding("MatchesVanillaColorSet", new bool[] { true, true, true });
            m_CanPasteColor = CreateBinding("CanPasteColor", false);
            m_CanPasteColorSet = CreateBinding("CanPasteColorSet", false);
            m_SingleInstance = CreateBinding("SingleInstance", true);
            m_Minimized = CreateBinding("Minimized", false);
            m_DisableSingleInstance = CreateBinding("DisableSingleInstance", false);
            m_DisableMatching = CreateBinding("DisableMatching", false);
            m_MatchesSavedOnDisk = CreateBinding("MatchesSavedOnDisk", false);

            // These handle actions triggered by UI.
            CreateTrigger<int, UnityEngine.Color>("ChangeColor", ChangeColorAction);
            CreateTrigger<UnityEngine.Color>("CopyColor", CopyColor);
            CreateTrigger<int>("ResetColor", ResetColor);
            CreateTrigger<int>("PasteColor", PasteColor);
            CreateTrigger("CopyColorSet", CopyColorSet);
            CreateTrigger("PasteColorSet", PasteColorSet);
            CreateTrigger("ResetColorSet", ResetColorSet);
            CreateTrigger("SaveToDisk", SaveColorSetToDisk);
            CreateTrigger("RemoveFromDisk", RemoveFromDisk);
            CreateTrigger("ActivateColorPicker", () => m_ToolSystem.activeTool = m_ToolSystem.activeTool = m_ColorPickerTool);
            CreateTrigger("ActivateColorPainter", () =>
            {
                m_ToolSystem.selected = Entity.Null;
                m_ActivateColorPainter = true;
                if (Mod.Instance.Settings.ColorPainterAutomaticCopyColor)
                {
                    m_ColorPainterUISystem.ColorSet = m_CurrentColorSet.Value.GetColorSet();
                }
            });
            CreateTrigger("Minimize", () => m_Minimized.Value = !m_Minimized.Value);
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


            // For the keybinds
            m_ActivateColorPainterAction = Mod.Instance.Settings.GetAction(Setting.ActivateColorPainterActionName);
            m_ActivateColorPainterAction.shouldBeEnabled = true;
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode.IsGame())
            {
                GetClimatePrefab();
                Enabled = true;
                m_ReloadInXFrames = 30;
            }
            else
            {
                Enabled = false;
                return;
            }

            if (m_VanillaColorSets.Count > 0)
            {
                return;
            }

            NativeList<Entity> colorVariationPrefabEntities = m_SubMeshQuery.ToEntityListAsync(Allocator.Temp, out JobHandle colorVariationPrefabJobHandle);
            NativeList<Entity> prefabsNeedingUpdates = new NativeList<Entity>(Allocator.Temp);
            colorVariationPrefabJobHandle.Complete();

            foreach (Entity e in colorVariationPrefabEntities)
            {
                if (!EntityManager.TryGetBuffer(e, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    continue;
                }

                int length = subMeshBuffer.Length;
                if (EntityManager.HasComponent<Tree>(e))
                {
                    length = Math.Min(4, subMeshBuffer.Length);
                }

                for (int i = 0; i < length; i++)
                {
                    if (!EntityManager.TryGetBuffer(subMeshBuffer[i].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer))
                    {
                        continue;
                    }

                    PrefabBase prefabBase = m_PrefabSystem.GetPrefab<PrefabBase>(subMeshBuffer[i].m_SubMesh);
                    PrefabID prefabID = prefabBase.GetPrefabID();

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

                        if (!m_VanillaColorSets.ContainsKey(assetSeasonIdentifier))
                        {
                            m_VanillaColorSets.Add(assetSeasonIdentifier, currentColorVariation.m_ColorSet);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (m_ReloadInXFrames > 0)
            {
                m_ReloadInXFrames--;
                if (m_ReloadInXFrames == 0)
                {
                    ReloadSavedColorSetsFromDisk();
                    m_CustomColorVariationSystem.ReloadCustomColorVariations(m_EndFrameBarrier.CreateCommandBuffer());
                }
            }

            // This was how I resolved an issue where I wanted to unselect an entity before activating a tool.
            if (selectedEntity == Entity.Null && m_ActivateColorPainter)
            {
                m_ActivateColorPainter = false;
                m_ToolSystem.activeTool = m_ColorPainterTool;
            }

            if (m_ActivateColorPainterAction.WasPerformedThisFrame())
            {
                m_ActivateColorPainter = true;
                if (selectedEntity != Entity.Null)
                {
                    m_ToolSystem.selected = Entity.Null;
                    if (Mod.Instance.Settings.ColorPainterAutomaticCopyColor)
                    {
                        m_ColorPainterUISystem.ColorSet = m_CurrentColorSet.Value.GetColorSet();
                    }
                }

                visible = false;
                m_PreviouslySelectedEntity = Entity.Null;

                return;
            }

            if (selectedEntity == Entity.Null && m_PreviouslySelectedEntity != Entity.Null)
            {
                m_PreviouslySelectedEntity = Entity.Null;
            }

            if (selectedEntity == Entity.Null)
            {
                if (m_DisableMatching.Value)
                {
                    m_DisableMatching.Value = false;
                }

                if (m_DisableSingleInstance.Value)
                {
                    m_DisableSingleInstance.Value = false;
                }

                visible = false;
                return;
            }

            if (m_PreviouslySelectedEntity == Entity.Null)
            {
                visible = false;
            }

            if (m_PreviouslySelectedEntity != selectedEntity)
            {
                m_PreviouslySelectedEntity = Entity.Null;
            }

            bool foundClimatePrefab = true;
            if (m_ClimatePrefab is null)
            {
                foundClimatePrefab = GetClimatePrefab();
            }

            m_CurrentEntity = selectedEntity;
            m_CurrentPrefabEntity = selectedPrefab;
            if (!m_PrefabSystem.TryGetPrefab(selectedPrefab, out PrefabBase currentPrefabBase))
            {
                visible = false;
                return;
            }


            if (EntityManager.HasComponent<Game.Tools.EditorContainer>(selectedEntity)
                && EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> ownerBuffer)
                && ownerBuffer.Length == 1
                && EntityManager.TryGetComponent(ownerBuffer[0].m_SubLane, out PrefabRef prefabRef)
                && m_PrefabSystem.TryGetPrefab(prefabRef.m_Prefab, out currentPrefabBase)
                && m_PrefabSystem.TryGetEntity(currentPrefabBase, out m_CurrentPrefabEntity))
            {
                m_CurrentEntity = ownerBuffer[0].m_SubLane;
            }

            if (!EntityManager.TryGetBuffer(m_CurrentEntity, isReadOnly: true, out DynamicBuffer<MeshColor> meshColorBuffer))
            {
                visible = false;
                return;
            }

            ColorSet originalMeshColor = meshColorBuffer[0].m_ColorSet;
            if (EntityManager.TryGetComponent(m_CurrentEntity, out Game.Objects.Tree tree))
            {
                if (tree.m_State == Game.Objects.TreeState.Dead || tree.m_State == Game.Objects.TreeState.Collected || tree.m_State == Game.Objects.TreeState.Stump)
                {
                    visible = false;
                    return;
                }

                if ((int)tree.m_State > 0)
                {
                    originalMeshColor = meshColorBuffer[(int)Math.Log((int)tree.m_State, 2) + 1].m_ColorSet;
                }
            }

            if (EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) && !m_DisableMatching)
            {
                m_DisableMatching.Value = true;
            }
            else if (!EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) && m_DisableMatching)
            {
                m_DisableMatching.Value = false;
            }

            if (EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity) && !m_DisableSingleInstance)
            {
                m_DisableSingleInstance.Value = true;
            }
            else if (!EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity) && m_DisableSingleInstance)
            {
                m_DisableSingleInstance.Value = false;
            }

            // Colors Variation
            if (m_PreviouslySelectedEntity != m_CurrentEntity
                && m_CurrentEntity != Entity.Null
                && m_CurrentPrefabEntity != Entity.Null
                && (!m_SingleInstance.Value || EntityManager.HasComponent<Plant>(m_CurrentEntity))
                && !EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity)
                && foundClimatePrefab
                && EntityManager.TryGetBuffer(m_CurrentPrefabEntity, isReadOnly: true, out DynamicBuffer<SubMesh> subMeshBuffer)
                && EntityManager.TryGetBuffer(subMeshBuffer[0].m_SubMesh, isReadOnly: true, out DynamicBuffer<ColorVariation> colorVariationBuffer)
                && colorVariationBuffer.Length > 0)
            {
                Season currentSeason = GetSeasonFromSeasonID(m_ClimatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);

                ColorSet colorSet = colorVariationBuffer[0].m_ColorSet;
                int index = 0;
                float cummulativeDifference = float.MaxValue;
                Season season = Season.None;
                for (int i = 0; i < colorVariationBuffer.Length; i++)
                {
                    if ((TryGetSeasonFromColorGroupID(colorVariationBuffer[i].m_GroupID, out Season checkSeason) && checkSeason == currentSeason) || checkSeason == Season.None)
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
                    visible = false;
                    return;
                }

                visible = true;
                m_CurrentAssetSeasonIdentifier = new AssetSeasonIdentifier()
                {
                    m_Index = index,
                    m_PrefabID = prefabBase.GetPrefabID(),
                    m_Season = season,
                };

                m_CurrentColorSet.Value = new RecolorSet(colorSet);

                m_PreviouslySelectedEntity = m_CurrentEntity;

                m_MatchesVanillaColorSet.Value = MatchesVanillaColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
                m_MatchesSavedOnDisk.Value = MatchesSavedOnDiskColorSet(colorSet, m_CurrentAssetSeasonIdentifier);
            }

            if (m_PreviouslySelectedEntity != m_CurrentEntity
                && (m_SingleInstance || EntityManager.HasComponent<CustomMeshColor>(m_CurrentEntity))
                && meshColorBuffer.Length > 0)
            {
                m_CurrentColorSet.Value = new RecolorSet(meshColorBuffer[0].m_ColorSet);
                m_PreviouslySelectedEntity = m_CurrentEntity;
                m_MatchesVanillaColorSet.Value = EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) ? new bool[] { false, false, false } : new bool[] { true, true, true };

                visible = true;
            }

            if (m_PreviouslySelectedEntity == m_CurrentEntity &&
                (!m_SingleInstance.Value || EntityManager.HasComponent<Game.Objects.Plant>(m_CurrentEntity)) &&
                !EntityManager.HasBuffer<CustomMeshColor>(m_CurrentEntity) &&
                m_NeedsColorRefresh == true &&
                UnityEngine.Time.time > m_TimeColorLastChanged + 0.5f)
            {
                ColorRefresh();
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
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase prefabBase))
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
                    if (!m_PrefabSystem.TryGetPrefab(subMeshBuffer[i].m_SubMesh, out PrefabBase prefabBase))
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

                    AssetSeasonIdentifier assetSeasonIdentifier = new AssetSeasonIdentifier()
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
            SavedColorSet customColorSet = new SavedColorSet(colorSet, assetSeasonIdentifier);

            try
            {
                XmlSerializer serTool = new XmlSerializer(typeof(SavedColorSet)); // Create serializer
                using (System.IO.FileStream file = System.IO.File.Create(colorDataFilePath)) // Create file
                {
                    serTool.Serialize(file, customColorSet); // Serialize whole properties
                }

                m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TrySaveCustomColorSetToDisk)} saved color set for {assetSeasonIdentifier.m_PrefabID}.");
                return true;
            }
            catch (Exception ex)
            {
                m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TrySaveCustomColorSetToDisk)} Could not save values for {assetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
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

                    AssetSeasonIdentifier assetSeasonIdentifier = new AssetSeasonIdentifier()
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
                    m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TryDeleteSavedColorSetFile)} Could not delete file for Set {assetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
                }
            }
            else
            {
                m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TryDeleteSavedColorSetFile)} Could not find file for {assetSeasonIdentifier.m_PrefabID} {assetSeasonIdentifier.m_Season} {assetSeasonIdentifier.m_Index} at {GetAssetSeasonIdentifierFilePath(m_CurrentAssetSeasonIdentifier)}");
            }

        }

        private void ResetColor(int channel)
        {
            EntityCommandBuffer buffer = m_EndFrameBarrier.CreateCommandBuffer();

            if (EntityManager.HasComponent<CustomMeshColor>(m_CurrentEntity))
            {
                buffer.RemoveComponent<CustomMeshColor>(m_CurrentEntity);
                buffer.AddComponent<BatchesUpdated>(m_CurrentEntity);

                m_PreviouslySelectedEntity = Entity.Null;
                return;
            }

            if (!TryGetVanillaColorSet(m_CurrentAssetSeasonIdentifier, out ColorSet vanillaColorSet))
            {
                m_Log.Info($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(ResetColor)} Could not find vanilla color set for {m_CurrentAssetSeasonIdentifier.m_PrefabID} {m_CurrentAssetSeasonIdentifier.m_Season} {m_CurrentAssetSeasonIdentifier.m_Index}");
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

            }
            else
            {
                GenerateOrUpdateCustomColorVariationEntity();
                return;
            }

            ColorRefresh();
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
                    XmlSerializer serTool = new XmlSerializer(typeof(SavedColorSet)); // Create serializer
                    using System.IO.FileStream readStream = new System.IO.FileStream(colorDataFilePath, System.IO.FileMode.Open); // Open file
                    result = (SavedColorSet)serTool.Deserialize(readStream); // Des-serialize to new Properties

                    // m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TryLoadCustomColorSet)} loaded color set for {assetSeasonIdentifier.m_PrefabID}.");
                    return true;
                }
                catch (Exception ex)
                {
                    m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(TryLoadCustomColorSetFromDisk)} Could not get default values for Set {assetSeasonIdentifier.m_PrefabID}. Encountered exception {ex}");
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
                }
            }

            m_NeedsColorRefresh = false;
        }

        private void ReloadSavedColorSetsFromDisk()
        {
            string[] filePaths = Directory.GetFiles(m_ContentFolder);
            NativeList<Entity> prefabsNeedingUpdates = new NativeList<Entity>(Allocator.Temp);
            foreach (string filePath in filePaths)
            {
                SavedColorSet colorSet = default;
                if (File.Exists(filePath))
                {
                    try
                    {
                        XmlSerializer serTool = new XmlSerializer(typeof(SavedColorSet)); // Create serializer
                        using System.IO.FileStream readStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open); // Open file
                        colorSet = (SavedColorSet)serTool.Deserialize(readStream);
                    }
                    catch (Exception ex)
                    {
                        m_Log.Warn($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(ReloadSavedColorSetsFromDisk)} Could not deserialize file at {filePath}. Encountered exception {ex}");
                        continue;
                    }
                }

                PrefabID prefabID = new PrefabID(colorSet.PrefabType, colorSet.PrefabName);

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

                        m_Log.Debug($"{nameof(SelectedInfoPanelColorFieldsSystem)}.{nameof(OnGameLoadingComplete)} Imported Colorset for {prefabID} in {assetSeasonIdentifier.m_Season}");
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
                }
            }
        }

        /// <summary>
        /// Identifies an asset and color variation including seasonal color variations..
        /// </summary>
        public struct AssetSeasonIdentifier
        {
            /// <summary>
            /// The id for the prefab.
            /// </summary>
            public PrefabID m_PrefabID;

            /// <summary>
            /// The season or none.
            /// </summary>
            public Season m_Season;

            /// <summary>
            /// The index of the color variation.
            /// </summary>
            public int m_Index;
        }
    }
}
