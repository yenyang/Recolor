// <copyright file="Mod.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

// #define DUMP_VANILLA_LOCALIZATION
#define EXPORT_EN_US
namespace Recolor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.Rendering;
    using Game.SceneFlow;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.Palettes;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.SingleInstance;
    using Recolor.Systems.Tools;
    using Recolor.Systems.Vehicles;

#if DEBUG
    using Newtonsoft.Json;
    using Colossal;
    using Game.UI.InGame;
    using Unity.Entities;
    using UnityEngine;
    using Recolor.Systems.Palettes;
#endif

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

        /// <summary>
        /// Gets the install path for the mod.
        /// </summary>
        internal string InstallPath { get; private set; }

        /// <inheritdoc/>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            Log = LogManager.GetLogger("Mods_Yenyang_" + Id).SetShowsErrorsInUI(false);
#if DEBUG
            Log.effectivenessLevel = Level.Debug;
#elif VERBOSE
            Log.effectivenessLevel = Level.Verbose;
#else
            Log.effectivenessLevel = Level.Info;
#endif
            Log.Info($"{nameof(OnLoad)} Version: " + Version);
            Log.Info($"{nameof(OnLoad)} Initalizing Settings");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                Log.Info($"Current mod asset at {asset.path}");
                InstallPath = asset.path;
            }

            Settings = new Setting(this);
            Settings.RegisterKeyBindings();
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(Recolor), Settings, new Setting(this));
            Log.Info($"{nameof(OnLoad)} Initalizing en-US localization.");
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            Log.Info($"[{nameof(Mod)}] {nameof(OnLoad)} Initalizing localization for other languages.");
            LoadNonEnglishLocalizations();
#if DEBUG && EXPORT_EN_US
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
#if DUMP_VANILLA_LOCALIZATION
            var strings = GameManager.instance.localizationManager.activeDictionary.entries
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var json = Colossal.Json.JSON.Dump(strings);

            var filePath = Path.Combine(Application.persistentDataPath, "locale-dictionary.json");

            File.WriteAllText(filePath, json);
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
            updateSystem.UpdateAfter<CreatedServiceVehicleCustomColorSystem, MeshColorSystem>(SystemUpdatePhase.PreCulling);
            updateSystem.UpdateAfter<AssignedRouteVehicleCustomColorSystem, MeshColorSystem>(SystemUpdatePhase.PreCulling);
            updateSystem.UpdateAt<PalettesUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<AddPalettePrefabsSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateAfter<AssignedPaletteCustomColorSystem, MeshColorSystem>(SystemUpdatePhase.PreCulling);
            updateSystem.UpdateAt<PaletteInstanceManagerSystem>(SystemUpdatePhase.ModificationEnd);
            updateSystem.UpdateAt<PalettesDuringPlacementSystem>(SystemUpdatePhase.Modification2);
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

        private void LoadNonEnglishLocalizations()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = thisAssembly.GetManifestResourceNames();

            try
            {
                Log.Debug($"Reading localizations");

                foreach (string localeID in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    string resourceName = $"{thisAssembly.GetName().Name}.l10n.{localeID}.json";
                    if (resourceNames.Contains(resourceName))
                    {
                        Log.Debug($"Found localization file {resourceName}");
                        try
                        {
                            Log.Debug($"Reading embedded translation file {resourceName}");

                            // Read embedded file.
                            using StreamReader reader = new (thisAssembly.GetManifestResourceStream(resourceName));
                            {
                                string entireFile = reader.ReadToEnd();
                                Colossal.Json.Variant varient = Colossal.Json.JSON.Load(entireFile);
                                Dictionary<string, string> translations = varient.Make<Dictionary<string, string>>();
                                GameManager.instance.localizationManager.AddSource(localeID, new MemorySource(translations));
                            }
                        }
                        catch (Exception e)
                        {
                            // Don't let a single failure stop us.
                            Log.Error(e, $"Exception reading localization from embedded file {resourceName}");
                        }
                    }
                    else
                    {
                        Log.Debug($"Did not find localization file {resourceName}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception reading embedded settings localization files");
            }
        }
    }
}
