// <copyright file="SIPColorFieldsSystem.Initialization.cs" company="Yenyang's Mods. MIT License">
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

    /// <summary>
    /// Adds color fields to selected info panel for changing colors of buildings, vehicles, props, etc.
    /// </summary>
    public partial class SIPColorFieldsSystem : ExtendedInfoSectionBase
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
        private ValueBindingHelper<int> m_SubMeshIndex;
        private ValueBindingHelper<ButtonState> m_SingleInstance;
        private ValueBindingHelper<ButtonState> m_Matching;
        private ValueBindingHelper<ButtonState> m_ServiceVehicles;
        private ValueBindingHelper<ButtonState> m_Route;
        private ValueBindingHelper<bool> m_CanPasteColor;
        private ValueBindingHelper<bool> m_CanPasteColorSet;
        private ValueBindingHelper<bool> m_Minimized;
        private ValueBindingHelper<bool> m_ShowHexaDecimals;
        private Dictionary<AssetSeasonIdentifier, Game.Rendering.ColorSet> m_VanillaColorSets;
        private ValueBindingHelper<bool> m_MatchesSavedOnDisk;
        private ValueBindingHelper<bool> m_CanResetSingleChannels;
        private Scope m_PreferredScope;
        private ColorPickerToolSystem m_ColorPickerTool;
        private ColorPainterToolSystem m_ColorPainterTool;
        private DefaultToolSystem m_DefaultToolSystem;
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
        private int m_RouteColorChannel = -1;

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
        /// An enum to handle whether a button is selected and/or hidden.
        /// </summary>
        public enum ButtonState
        {
            /// <summary>
            /// Not selected.
            /// </summary>
            Off = 0,

            /// <summary>
            /// Selected.
            /// </summary>
            On = 1,

            /// <summary>
            /// Not shown.
            /// </summary>
            Hidden = 2,
        }

        /// <summary>
        /// This is the preferred group of entities to change. Others will be selected be default when applicable.
        /// </summary>
        public enum Scope
        {
            /// <summary>
            /// Single instance entity.
            /// </summary>
            SingleInstance = 0,

            /// <summary>
            /// All matching meshes.
            /// </summary>
            Matching = 1,

            /// <summary>
            /// All service vehicles from same service building.
            /// </summary>
            ServiceVehicles = 2,

            /// <summary>
            /// All vehicles on the same route.
            /// </summary>
            Route = 3,
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
            m_Log = Mod.Instance.Log;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Log.Info($"{nameof(SIPColorFieldsSystem)}.{nameof(OnCreate)}");
            m_ClimateSystem = World.GetOrCreateSystemManaged<ClimateSystem>();
            m_ColorPainterTool = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_ColorPainterUISystem = World.GetOrCreateSystemManaged<ColorPainterUISystem>();
            m_ColorPickerTool = World.GetOrCreateSystemManaged<ColorPickerToolSystem>();
            m_CustomColorVariationSystem = World.GetOrCreateSystemManaged<CustomColorVariationSystem>();
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();

            // These establish bindings to communicate between UI and C#.
            m_CurrentColorSet = CreateBinding("CurrentColorSet", new RecolorSet(default, default, default));
            m_MatchesVanillaColorSet = CreateBinding("MatchesVanillaColorSet", new bool[] { true, true, true });
            m_CanPasteColor = CreateBinding("CanPasteColor", false);
            m_CanPasteColorSet = CreateBinding("CanPasteColorSet", false);
            m_Minimized = CreateBinding("Minimized", false);
            m_MatchesSavedOnDisk = CreateBinding("MatchesSavedOnDisk", false);
            m_ShowHexaDecimals = CreateBinding("ShowHexaDecimals", Mod.Instance.Settings.ShowHexaDecimals);
            m_SubMeshIndex = CreateBinding("SubMeshIndex", 0);
            m_CanResetSingleChannels = CreateBinding("CanResetSingleChannels", false);

            // These bindings are closely related.
            m_PreferredScope = Scope.SingleInstance;
            m_SingleInstance = CreateBinding("SingleInstance", ButtonState.On);
            m_ServiceVehicles = CreateBinding("ServiceVehicles", ButtonState.Off | ButtonState.Hidden);
            m_Matching = CreateBinding("Matching", ButtonState.Off);
            m_Route = CreateBinding("Route", ButtonState.Off | ButtonState.Hidden);

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
            CreateTrigger("ChangeScope", (int newScope) =>
            {
                m_PreferredScope = (Scope)newScope;
                HandleScopeAndButtonStates();
                m_PreviouslySelectedEntity = Entity.Null;
            });
            CreateTrigger("ToggleShowHexaDecimals", () =>
            {
                m_ShowHexaDecimals.Value = !m_ShowHexaDecimals.Value;
                Mod.Instance.Settings.ShowHexaDecimals = m_ShowHexaDecimals.Value;
                Mod.Instance.Settings.ApplyAndSave();
            });

            m_VanillaColorSets = new ();
            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id, "SavedColorSet", "Custom");
            System.IO.Directory.CreateDirectory(m_ContentFolder);
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            m_SubMeshQuery = SystemAPI.QueryBuilder().WithAll<Game.Prefabs.SubMesh>().WithNone<Game.Prefabs.PlaceholderObjectElement, Game.Common.Deleted>().Build();

            RequireForUpdate(m_SubMeshQuery);
            Enabled = false;

            // For the keybinds
            m_ActivateColorPainterAction = Mod.Instance.Settings.GetAction(Setting.ActivateColorPainterActionName);
            m_ActivateColorPainterAction.shouldBeEnabled = true;
        }

        /// <summary>
        /// Identifies an asset and color variation including seasonal color variations..
        /// </summary>
        public struct AssetSeasonIdentifier
        {
            /// <summary>
            /// The id for the prefab.
            /// </summary>
            public Game.Prefabs.PrefabID m_PrefabID;

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
