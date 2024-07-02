namespace Recolor.Settings
{
    using Colossal.IO.AssetDatabase;
    using Game.Input;
    using Game.Modding;
    using Game.Settings;
    using Recolor.Systems;
    using Unity.Entities;

    /// <summary>
    /// Settings class for Recolor mod.
    /// </summary>
    [FileLocation("ModsSettings/" + nameof(Recolor) + "/" + nameof(Recolor))]
    [SettingsUIGroupOrder(General, Keybinds, Remove, About)]
    public class Setting : ModSetting
    {
        /// <summary>
        /// This is for general settings.
        /// </summary>
        public const string General = "General";

        /// <summary>
        /// This is for general settings.
        /// </summary>
        public const string Keybinds = "Keybinds";

        /// <summary>
        /// This is for about section.
        /// </summary>
        public const string About = "About";

        /// <summary>
        /// This is for reseting settings button group.
        /// </summary>
        public const string Remove = "Remove";

        /// <summary>
        /// The action name for toggle color painter keybind.
        /// </summary>
        public const string ActivateColorPainterActionName = "ActivateColorPainter";


        /// <summary>
        /// Initializes a new instance of the <see cref="Setting"/> class.
        /// </summary>
        /// <param name="mod">Mod file.</param>
        public Setting(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        /// <summary>
        /// Gets or sets a value indicating whether to copy color while activating color painter.
        /// </summary>
        [SettingsUISection(General, General)]
        public bool ColorPainterAutomaticCopyColor { get; set; }

        /// <summary>
        /// Sets a value indicating whether to reset all settings to default.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(General, General)]
        public bool ResetSettings
        {
            set
            {
                SetDefaults();
                ApplyAndSave();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the keybinding for Reset Elevation.
        /// </summary>
        [SettingsUISection(General, Keybinds)]
        [SettingsUIKeyboardBinding(UnityEngine.InputSystem.Key.P, actionName: ActivateColorPainterActionName, shift: true)]
        public ProxyBinding ActivateColorPainter { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for keybinds.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(General, Keybinds)]
        public bool ResetKeybindSettings
        {
            set
            {
                ResetKeyBindings();
            }
        }


        /// <summary>
        /// Sets a value indicating whether to Reset All Single Instance Color Changes.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(General, Remove)]
        public bool ResetAllSingleInstanceColorChanges
        {
            set
            {
                ResetCustomMeshColorSystem resetCustomMeshColorSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ResetCustomMeshColorSystem>();
                resetCustomMeshColorSystem.Enabled = true;
            }
        }

        /// <summary>
        /// Sets a value indicating whether to Reset ColorVariations In This SaveGame.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(General, Remove)]
        public bool ResetColorVariationsInThisSaveGame
        {
            set
            {
                CustomColorVariationSystem customColorVariationSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<CustomColorVariationSystem>();
                customColorVariationSystem.ResetAllCustomColorVariations();
            }
        }

        /// <summary>
        /// Sets a value indicating whether to Delete ModsData Saved Color Variations.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(General, Remove)]
        public bool DeleteModsDataSavedColorVariations
        {
            set
            {
                SelectedInfoPanelColorFieldsSystem selectedInfoPanelColorFieldsSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<SelectedInfoPanelColorFieldsSystem>();
                selectedInfoPanelColorFieldsSystem.DeleteAllModsDataFiles();
            }
        }

        /// <summary>
        /// Sets a value indicating whether to safely remove the mod.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(General, Remove)]
        public bool SafelyRemove
        {
            set
            {
                ResetCustomMeshColorSystem resetCustomMeshColorSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ResetCustomMeshColorSystem>();
                resetCustomMeshColorSystem.Enabled = true;
                CustomColorVariationSystem customColorVariationSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<CustomColorVariationSystem>();
                customColorVariationSystem.ResetAllCustomColorVariations();
                SelectedInfoPanelColorFieldsSystem selectedInfoPanelColorFieldsSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<SelectedInfoPanelColorFieldsSystem>();
                selectedInfoPanelColorFieldsSystem.DeleteAllModsDataFiles();
            }
        }

        /// <summary>
        /// Gets a value indicating the version.
        /// </summary>
        [SettingsUISection(General, About)]
        public string Version => Mod.Instance.Version;

        /// <inheritdoc/>
        public override void SetDefaults()
        {
            ColorPainterAutomaticCopyColor = true;
        }
    }
}
