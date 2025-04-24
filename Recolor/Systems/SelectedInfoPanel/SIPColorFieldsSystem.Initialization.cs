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
    /// Adds color fields to selected info panel for changing colors of buildings, vehicles, props, etc.
    /// </summary>
    public partial class SIPColorFieldsSystem : ExtendedInfoSectionBase
    {
        /// <summary>
        /// Category name for no subcategory.
        /// </summary>
        public const string NoSubcategoryName = "No Subcategory";

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
        private ValueBindingHelper<SubMeshData> m_SubMeshData;
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
        private ValueBindingHelper<bool> m_EditorVisible;
        private ValueBindingHelper<bool> m_ResidentialBuildingSelected;
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
        private List<int> m_SubMeshIndexes = new List<int>();
        private ValueBindingHelper<bool> m_CanResetOtherSubMeshes;
        private bool m_PreferPalettes = false;
        private ValueBindingHelper<ButtonState> m_ShowPaletteChoices;
        private PalettesUISystem m_PalettesUISystem;
        private PaletteInstanceManagerSystem m_PaletteInstanceMangerSystem;
        private ValueBindingHelper<PaletteChooserUIData> m_PaletteChooserData;
        private AssignedPaletteCustomColorSystem m_AssignedPaletteCustomColorSystem;
        private EntityQuery m_PaletteQuery;
        private ValueBindingHelper<Entity> m_CopiedPalette;
        private ValueBindingHelper<Entity[]> m_CopiedPaletteSet;
        private EntityQuery m_AssetPackQuery;
        private EntityQuery m_ZonePrefabEntityQuery;
        private EntityQuery m_ThemePrefabEntityQuery;

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
        public override GameMode gameMode => GameMode.GameOrEditor;

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
            m_PalettesUISystem = World.GetOrCreateSystemManaged<PalettesUISystem>();
            m_PaletteInstanceMangerSystem = World.GetOrCreateSystemManaged<PaletteInstanceManagerSystem>();
            m_AssignedPaletteCustomColorSystem = World.GetOrCreateSystemManaged<AssignedPaletteCustomColorSystem>();

            // These establish bindings to communicate between UI and C#.
            m_CurrentColorSet = CreateBinding("CurrentColorSet", new RecolorSet(default, default, default));
            m_MatchesVanillaColorSet = CreateBinding("MatchesVanillaColorSet", new bool[] { true, true, true });
            m_CanPasteColor = CreateBinding("CanPasteColor", false);
            m_CanPasteColorSet = CreateBinding("CanPasteColorSet", false);
            m_Minimized = CreateBinding("Minimized", Mod.Instance.Settings.Minimized || Mod.Instance.Settings.AlwaysMinimizedAtGameStart);
            m_MatchesSavedOnDisk = CreateBinding("MatchesSavedOnDisk", false);
            m_ShowHexaDecimals = CreateBinding("ShowHexaDecimals", Mod.Instance.Settings.ShowHexaDecimals);
            m_SubMeshData = CreateBinding("SubMeshData", new SubMeshData(0, 1, string.Empty, SubMeshData.SubMeshScopes.All, ButtonState.Off, ButtonState.Off, ButtonState.On));
            m_CanResetSingleChannels = CreateBinding("CanResetSingleChannels", false);
            m_CanResetOtherSubMeshes = CreateBinding("CanResetOtherSubMeshes", false);
            m_EditorVisible = CreateBinding("EditorVisible", false);
            m_PaletteChooserData = CreateBinding("PaletteChooserData", new PaletteChooserUIData());
            m_ShowPaletteChoices = CreateBinding("ShowPaletteChoices", ButtonState.Off);
            m_CopiedPalette = CreateBinding("CopiedPalette", Entity.Null);
            m_CopiedPaletteSet = CreateBinding("CopiedPaletteSet", new Entity[] { Entity.Null, Entity.Null, Entity.Null });
            m_ResidentialBuildingSelected = CreateBinding("ResidentialBuildingSelected", false);

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
            CreateTrigger("ActivateColorPicker", () =>
            {
                m_ToolSystem.activeTool = m_ToolSystem.activeTool = m_ColorPickerTool;
                if (m_ToolSystem.actionMode.IsEditor())
                {
                    m_EditorVisible.Value = false;
                }
            });
            CreateTrigger("ActivateColorPainter", () =>
            {
                m_ToolSystem.selected = Entity.Null;
                m_ActivateColorPainter = true;
                if (Mod.Instance.Settings.ColorPainterAutomaticCopyColor)
                {
                    m_ColorPainterUISystem.ColorSet = m_CurrentColorSet.Value.GetColorSet();
                }
            });
            CreateTrigger("Minimize", () =>
            {
                m_Minimized.Value = !m_Minimized.Value;
                Mod.Instance.Settings.Minimized = m_Minimized.Value;
                Mod.Instance.Settings.ApplyAndSave();
            });
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
            CreateTrigger("ReduceSubMeshIndex", ReduceSubMeshIndex);
            CreateTrigger("IncreaseSubMeshIndex", IncreaseSubMeshIndex);

            CreateTrigger("ChangeSubMeshScope", (int newScope) =>
            {
                m_SubMeshData.Value.SubMeshScope = (SubMeshData.SubMeshScopes)newScope;
                HandleSubMeshScopes();
                m_PreviouslySelectedEntity = Entity.Null;
            });
            CreateTrigger("ToggleShowPaletteChoices", () =>
            {
                m_PreferPalettes = !m_PreferPalettes;
                HandleScopeAndButtonStates();
            });
            CreateTrigger<int, Entity>("AssignPalette", AssignPaletteAction);
            CreateTrigger<int>("RemovePalette", RemovePaletteAction);
            CreateTrigger("CopyPalette", (Entity prefabEntity) => m_CopiedPalette.Value = prefabEntity);
            CreateTrigger("EditPalette", (Entity prefabEntity) => m_PalettesUISystem.EditPalette(prefabEntity));
            CreateTrigger("CopyPaletteSet", (Entity[] prefabEntities) => m_CopiedPaletteSet.Value = prefabEntities);

            m_VanillaColorSets = new ();
            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id, "SavedColorSet", "Custom");
            System.IO.Directory.CreateDirectory(m_ContentFolder);

            m_SubMeshQuery = SystemAPI.QueryBuilder()
                .WithAll<Game.Prefabs.SubMesh>()
                .WithNone<Game.Prefabs.PlaceholderObjectElement, Game.Common.Deleted>()
                .Build();

            m_PaletteQuery = SystemAPI.QueryBuilder()
                  .WithAll<SwatchData>()
                  .WithNone<Deleted, Temp>()
                  .Build();

            m_AssetPackQuery = SystemAPI.QueryBuilder()
                .WithAll<AssetPackData>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_ThemePrefabEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<ThemeData>()
                .WithNone<Deleted, Temp>()
                .Build();

            m_ZonePrefabEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<ZoneData, UIObjectData>()
                .WithNone<Deleted, Temp>()
                .Build();

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
