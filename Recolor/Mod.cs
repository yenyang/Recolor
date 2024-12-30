// <copyright file="Mod.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor
{
    using System.Reflection;
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.Rendering;
    using Game.SceneFlow;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.SingleInstance;
    using Recolor.Systems.Tools;

#if DEBUG
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Colossal;
#endif

    /// <summary>
    /// Mod entry point.
    /// </summary>
    public class Mod : IMod
    {

        /// <summary>
        /// Fake keybind action for apply.
        /// </summary>
        public const string PickerApplyMimicAction = "PickerApplyMimic";

        /// <summary>
        /// Fake keybind action for apply.
        /// </summary>
        public const string PainterApplyMimicAction = "PainterApplyMimic";

        /// <summary>
        /// Fake keybind action for secondary apply.
        /// </summary>
        public const string PainterSecondaryApplyMimicAction = "PainterSecondaryApplyMimic";

        /// <summary>
        /// Fake keybind action for apply.
        /// </summary>
        public const string SelectNetLaneFencesToolApplyMimicAction = "SelectNetLaneFencesToolApplyMimic";

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
            Log.Info($"{nameof(OnLoad)} Version: " + Version);
            Log.Info($"{nameof(OnLoad)} Initalizing Settings");

            Settings = new Setting(this);
            Settings.RegisterKeyBindings();
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(Recolor), Settings, new Setting(this));
            Log.Info($"{nameof(OnLoad)} Initalizing en-US localization.");
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
#if DEBUG
            Log.Info($"{nameof(Mod)}.{nameof(OnLoad)} Exporting localization");
            var localeDict = new LocaleEN(Settings).ReadEntries(new List<IDictionaryEntryError>(), new Dictionary<string, int>()).ToDictionary(pair => pair.Key, pair => pair.Value);
            var str = JsonConvert.SerializeObject(localeDict, Formatting.Indented);
            try
            {
                File.WriteAllText($"C:\\Users\\TJ\\source\\repos\\{Id}\\{Id}\\UI\\src\\mods\\lang\\en-US.json", str);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
#endif
            Log.Info($"{nameof(OnLoad)} Initalizing systems");
            updateSystem.UpdateAt<SIPColorFieldsSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<TempCustomMeshColorSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAfter<CustomMeshColorSystem, MeshColorSystem>(SystemUpdatePhase.PreCulling);
            updateSystem.UpdateAt<ColorPickerToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<ColorPainterToolSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<ColorPainterUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<GenericTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAt<CustomColorVariationSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<RentersUpdatedCustomMeshColorSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<ResetCustomMeshColorSystem>(SystemUpdatePhase.PreCulling);
            updateSystem.UpdateAt<SelectNetLaneFencesToolSystem>(SystemUpdatePhase.ToolUpdate);
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
