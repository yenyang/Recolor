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
            // Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(Recolor), Settings, new Setting(this));
            Log.Info($"{nameof(OnLoad)} Initalizing en-US localization.");
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            Log.Info($"{nameof(OnLoad)} Initalizing systems");
            updateSystem.UpdateAt<SelectedInfoPanelColorFieldsSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<TempCustomMeshColorSystem>(SystemUpdatePhase.Modification1);
            updateSystem.UpdateAfter<CustomMeshColorSystem, MeshColorSystem>(SystemUpdatePhase.PreCulling);
            updateSystem.UpdateBefore<TempCustomMeshColorSystem, MeshColorSystem>(SystemUpdatePhase.PreCulling);
            updateSystem.UpdateAt<ColorPickerAndPaintingTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateBefore<HandleBatchesUpdatedNextFrameSystem>(SystemUpdatePhase.Modification1);
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
