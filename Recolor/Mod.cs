// <copyright file="Mod.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Recolor
{
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.Rendering;
    using Game.SceneFlow;
    using Recolor.Settings;
    using Recolor.Systems;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// Mod entry point.
    /// </summary>
    public class Mod : IMod
    {
        /// <summary>
        /// An id used for bindings between UI and C#.
        /// </summary>
        public static readonly string Id = "Recolor";

        /// <summary>
        /// Gets the static reference to the mod instance.
        /// </summary>
        public static Mod Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the mods settings.
        /// </summary>
        internal Setting Settings { get; set; }

        /// <summary>
        /// Gets ILog for mod.
        /// </summary>
        internal ILog Log { get; private set; }

        /// <summary>
        /// Gets the version of the mod.
        /// </summary>
        internal string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        /// <inheritdoc/>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            Log = LogManager.GetLogger(Id).SetShowsErrorsInUI(false);
#if DEBUG
            Log.effectivenessLevel = Level.Debug;
#elif VERBOSE
            Log.effectivenessLevel = Level.Verbose;
#else
            Log.effectivenessLevel = Level.Info;
#endif
            Log.Info($"{nameof(OnLoad)} Initalizing Settings");

            Settings = new Setting(this);
            Settings.RegisterKeyBindings();
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(Recolor), Settings, new Setting(this));
            Log.Info($"{nameof(OnLoad)} Initalizing en-US localization.");
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            Log.Info($"{nameof(OnLoad)} Initalizing systems");
            updateSystem.UpdateAt<SelectedInfoPanelColorFieldsSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<TempCustomMeshColorSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<CustomMeshColorSystem, MeshColorSystem>(SystemUpdatePhase.PreCulling);
            updateSystem.UpdateAt<ColorPickerToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<ColorPainterToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<ColorPainterUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<GenericTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateBefore<HandleBatchesUpdatedNextFrameSystem>(SystemUpdatePhase.Modification1);
            updateSystem.UpdateAt<CustomColorVariationSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<RentersUpdatedCustomMeshColorSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<ResetCustomMeshColorSystem>(SystemUpdatePhase.PreCulling);
            Log.Info($"{nameof(OnLoad)} complete.");
        }

        /// <inheritdoc/>
        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}
