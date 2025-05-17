// <copyright file="LocaleEN.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Settings
{
    using System.Collections.Generic;
    using Colossal;
    using Colossal.IO.AssetDatabase.Internal;
    using Recolor;
    using Recolor.Systems.SelectedInfoPanel;

    /// <summary>
    /// Localization for <see cref="Setting"/> in English.
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        private Dictionary<string, string> m_Localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocaleEN"/> class.
        /// </summary>
        /// <param name="setting">Settings class.</param>
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;

            m_Localization = new Dictionary<string, string>()
            {
                { m_Setting.GetSettingsLocaleID(), Mod.Id },
                { SectionLabel("InfoRowTitle"), Mod.Id },
                { TooltipDescriptionKey("InfoRowTooltip"), "For choosing custom colors." },
                { SectionLabel("ColorSet"), "Color Set" },
                { TooltipDescriptionKey("Minimize"), "Minimize the Recolor panel to save space." },
                { TooltipDescriptionKey("Expand"), "Expands the Recolor panel for use." },
                { TooltipDescriptionKey("ColorPicker"), "Activates the color picker tool." },
                { TooltipDescriptionKey("ColorPainter"), "Activates the color painter tool." },
                { TooltipDescriptionKey("CopyColor"), "Copy a single color to paste later." },
                { TooltipDescriptionKey("PasteColor"), "Paste a single color as part of a color set." },
                { TooltipDescriptionKey("ResetColor"), "Reset a single color and saves the color back to the original color for this asset (and season, if applicable)." },
                { TooltipDescriptionKey("CopyColorSet"), "Copy the whole color set to paste later." },
                { TooltipDescriptionKey("PasteColorSet"), "Paste the copied color set." },
                { TooltipDescriptionKey("ResetColorSet"), "Reset the whole color set and saves the colors back to the original colors for this asset (and season, if applicable)." },
                { TooltipDescriptionKey("ResetInstanceColor"), "Resets the instance colors and this instance will return to utilizing its color variation." },
                { TooltipDescriptionKey("SaveToDisk"), "Saves the color variation to ModsData folder so that it can be used as default across multiple saves." },
                { TooltipDescriptionKey("RemoveFromDisk"), "Removes the saved color variation from ModsData folder so that it no longer is used across multiple saves." },
                { TooltipTitleKey("SingleInstance"), "Single Instance" },
                { TooltipDescriptionKey("SingleInstance"), "Change the colors of the current selection only." },
                { TooltipTitleKey("Matching"), "Matching Color Variation" },
                { TooltipDescriptionKey("Matching"), "Change the colors of all assets with the same mesh, color variation (and season, if applicable). Single Instance will override this for that instance." },
                { SectionLabel("Selection"), "Selection" },
                { TooltipTitleKey("SingleSelection"), "Single" },
                { TooltipDescriptionKey("SingleSelection"), "Changes the colors of one buildings, prop, or vehicle." },
                { TooltipTitleKey("RadiusSelection"), "Radius Selection" },
                { TooltipDescriptionKey("RadiusSelection"), "Changes the single instance colors of buildings, prop, and/or vehicles within the radius." },
                { TooltipDescriptionKey("IncreaseRadius"), "Increase the radius." },
                { TooltipDescriptionKey("DecreaseRadius"), "Decrease the radius." },
                { SectionLabel("Radius"), "Radius" },
                { SectionLabel("Filter"), "Filter" },
                { TooltipTitleKey("BuildingFilter"), "Building Filter" },
                { TooltipDescriptionKey("BuildingFilter"), "Color Painter tool will change single instance colors of buildings." },
                { TooltipTitleKey("PropFilter"), "Prop Filter" },
                { TooltipDescriptionKey("PropFilter"), "Color Painter tool will change single instance colors of props." },
                { TooltipTitleKey("VehicleFilter"), "Vehicle Filter" },
                { TooltipDescriptionKey("VehicleFilter"), "Color Painter tool will change single instance colors of vehicles." },
                { TooltipTitleKey("PaintToolMode"), "Paint" },
                { TooltipDescriptionKey("PaintToolMode"), "Left Click: Changes single instance colors or matching color variations on a selection. Right Click: Resets single instance colors back to color variation or custom color variations back to originals." },
                { TooltipTitleKey("ResetToolMode"), "Reset" },
                { TooltipDescriptionKey("ResetToolMode"), "Resets single instance colors back to color variation or custom color variations back to originals." },
                { TooltipTitleKey("PickerToolMode"), "Color Picker" },
                { TooltipDescriptionKey("PickerToolMode"), "Color picker within the painter tool." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ColorPainterAutomaticCopyColor)), "Automatically Copy Color Set when activating Color Painter" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ColorPainterAutomaticCopyColor)), "Copies and pastes the set of three colors from the selected asset whenever color painter is activated from the selected info panel button." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), $"Reset {Mod.Id} Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), $"Upon confirmation this will reset all settings for {Mod.Id} mod." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), $"Reset {Mod.Id} Settings?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(setting.Version)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(setting.Version)), $"Version number for the {Mod.Id} mod installed." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetAllSingleInstanceColorChanges)), $"Reset All Single Instance Color Changes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetAllSingleInstanceColorChanges)), $"Upon confirmation resets all instance colors in this save file." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetAllSingleInstanceColorChanges)), $"Reset All Single Instance Color Changes?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetColorVariationsInThisSaveGame)), $"Reset Color Variations in this Save Game" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetColorVariationsInThisSaveGame)), $"Upon confirmation removes all saved color variations from this save file." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetColorVariationsInThisSaveGame)), $"Reset Color Variations in this Save Game?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeleteModsDataSavedColorVariations)), $"Delete ModsData Saved Color Variations" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeleteModsDataSavedColorVariations)), $"Upon confirmation deletes all saved files in ModsData related to this mod." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.DeleteModsDataSavedColorVariations)), $"Delete ModsData Saved Color Variations?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SafelyRemove)), $"Safely Remove {Mod.Id}" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SafelyRemove)), $"Upon confirmation this will remove all components and entities from the {Mod.Id} mod. Resets all instance colors in this save file, remove all saved color variations from this save file, and delete all saved files in ModsData related to this mod." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.SafelyRemove)), $"Safely Remove {Mod.Id}?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ActivateColorPainter)), "Color Painter Tool Keybind" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ActivateColorPainter)), "A keybind to activate the color painter tool." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FenceSelectorMode)), "Activate NetLane Fence/Hedge Selector Mode" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FenceSelectorMode)), "Keyboard shortcut to activate the tool mode to select net lane fences and hedges." },
                { m_Setting.GetBindingMapLocaleID(), Mod.Id },
                { m_Setting.GetBindingKeyLocaleID(Setting.ActivateColorPainterActionName), "Color Painter Tool Activation key" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetKeybindSettings)), $"Reset {Mod.Id} mod Keybindings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetKeybindSettings)), $"Upon confirmation this will reset the keybindings for {Mod.Id} mod." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetKeybindSettings)), $"Reset {Mod.Id} mod Keybindings?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(setting.AlwaysMinimizedAtGameStart)), "Always Minimized at Game Start" },
                { m_Setting.GetOptionDescLocaleID(nameof(setting.AlwaysMinimizedAtGameStart)), "When enabled, the Recolor panel will always start minimized. When disabled, the Recolor panel will start minimized or not based on whether it was minimized or not when you last closed the game." },
                { MouseTooltipKey("SingleInstancePlantWarning"), "Single instance color changes for plants is not currently supported." },
                { MouseTooltipKey("HasCustomMeshColorWarning"),  "Cannot change color variation on this because it has custom instance colors." },
                { MouseTooltipKey("SelectANetLaneFence"), "Select a NetLane Fence or Hedge." },
                { TooltipDescriptionKey("ToggleChannel"), "Toggles the channel on or off." },
                { TooltipDescriptionKey("ShowHexaDecimals"), "Toggles whether Hexadecimal input fields are shown or not." },
                { TooltipDescriptionKey("SwapColors"), "Swaps colors between two channels." },
                { TooltipTitleKey("ServiceVehicles"), "Service Vehicles" },
                { TooltipDescriptionKey("ServiceVehicles"), "Changes the colors of all service vehicles owned by the same service building. Newly created service vehicles owned by the same building will also have the same color set." },
                { TooltipTitleKey("RouteVehicles"), "Route Vehicles" },
                { TooltipDescriptionKey("RouteVehicles"), "Changes the colors of all vehicles assigned to a route. Newly created vehicles on the route will also have the same color set." },
                { TooltipTitleKey("SingleSubMesh"), "Single Submesh" },
                { TooltipDescriptionKey("SingleSubMesh"), "Changes the colors of the individual submesh selected." },
                { TooltipTitleKey("MatchingSubmeshes"), "Matching Submeshes" },
                { TooltipDescriptionKey("MatchingSubmeshes"), "Changes the colors of submeshes with matching names." },
                { TooltipTitleKey("AllSubmeshes"), "All Submeshes" },
                { TooltipDescriptionKey("AllSubmeshes"), "Changes the colors of all submeshes." },
                { SectionLabel("SubMeshes"), "SubMeshes" },
                { SectionLabel("PaletteEditorMenu"), "Palette Editor Menu" },
                { SectionLabel("Palette"), "Palette" },
                { TooltipDescriptionKey("SavePalette"), "Save Palette" },
                { TooltipDescriptionKey("DeletePalette"), "Delete Palette" },
                { SectionLabel("Subcategory"), "Subcategory" },
                { TooltipDescriptionKey("EditSubcategory"), "Edit Subcategory" },
                { SectionLabel("FilterType"), "Filter Type" },
                { SectionLabel("FilterChoices"), "Filter Choices" },
                { TooltipDescriptionKey("AddSubcategory"), "Generate a new Subcategory." },
                { TooltipDescriptionKey("AddFilter"), "Add a compatible filter." },
                { TooltipDescriptionKey("RemoveFilter"), "Remove a compatible filter." },
                { SectionLabel("AddSwatch"), "Add a Swatch" },
                { TooltipDescriptionKey("AddSwatch"), "Generates an additional and randomly colored swatch." },
                { TooltipDescriptionKey("RemoveSwatch"), "Remove this swatch from the palette." },
                { TooltipDescriptionKey("RandomizeSwatch"), "Randomize the color of this swatch." },
                { TooltipDescriptionKey("ProbabilityWeight"), "Probability Weight" },
                { SectionLabel("Category"), "Category" },
                { TooltipDescriptionKey("BuildingCategory"), "Palette will be available to use for Buildings." },
                { TooltipDescriptionKey("VehicleCategory"), "Palette will be available to use for Vehicles." },
                { TooltipDescriptionKey("PropCategory"), "Palette will be available to use for Props." },
                { TooltipDescriptionKey("NetLaneCategory"), "Palette will be available to use for Net Lane Fences and Walls." },
                { TooltipDescriptionKey("AllCategories"), "Palette will not be limited based on category." },
                { TooltipDescriptionKey("AddLocale"), "Adds another set of input fields for entering translations for another language." },
                { SectionLabel("AddALocale"), "Add a Locale" },
                { SectionLabel("UniqueName"), "Unique Name" },
                { SectionLabel("None"), "None" },
                { TooltipDescriptionKey("EditPalette"), "Edit the selected palette." },
                { TooltipDescriptionKey("CopyPalette"), "Copy the selected palette to paste later." },
                { TooltipDescriptionKey("PastePalette"), "Paste the copied palette." },
                { TooltipDescriptionKey("SaveSubcategory"), "Save Subcategory" },
                { TooltipDescriptionKey("DeleteSubcategory"), "Delete Subcategory" },
                { SectionLabel("LocaleCode"), "Locale Code" },
                { SectionLabel("LocalizedName"), "Localized Name" },
                { SectionLabel("LocalizedDescription"), "Localized Description" },
                { TooltipDescriptionKey("RemoveALocale"), "Remove a Locale" },
                { NameKey(Systems.Palettes.PalettesUISystem.MenuType.Subcategory, SIPColorFieldsSystem.NoSubcategoryName), $"{SIPColorFieldsSystem.NoSubcategoryName}" },
                { TooltipDescriptionKey("SwapPalettes"), "Swap Palettes between channels." },
                { TooltipDescriptionKey("CopyPaletteSet"), "Copies Palette assignments for all 3 channels for pasting later." },
                { TooltipDescriptionKey("PastePaletteSet"), "Pastes Palette assignments for all 3 channels." },
                { TooltipDescriptionKey("GenerateNewPalette"), "Generate a new Palette and open the Palette Editor Menu." },
                { TooltipDescriptionKey("CloseEditorPanel"), "Close the Palette Editor Menu." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ShowPalettesOptionDuringPlacement)), "Show Palette options while placing objects" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ShowPalettesOptionDuringPlacement)), "Toggles whether to show dropdowns for picking palettes for the three color channels while placing objects with the Object tool." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetPaletteChoicesWhenSwitchingPrefab)), "Reset Palette choices when Selecting New Asset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetPaletteChoicesWhenSwitchingPrefab)), "Toggles whether to automatically reset palette choices to None whenever you change to a new asset selection." },
                { TooltipDescriptionKey("HidePalettesDuringPlacement"), "Hides the palette choosers while placing objects with the Object tool. Restore using the mod's settings." },
                { TooltipDescriptionKey("MinimizeDuringPlacement"), "Minimize the palette choosers while placing objects to save space." },
                { TooltipDescriptionKey("ExpandDuringPlacement"), "Expands the palette choosers while placing objects for use." },
                { TooltipDescriptionKey("ResetPalettesDuringPlacement"), "Resets the palette choosers while placing objects." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RestoreDefaultPalettes)), "Restore Default Palettes and Subcategories." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RestoreDefaultPalettes)), "On confirmation, will re-install original files for pre-built Palettes and Subcategories. Any edits will be lost. This cannot be undone. Custom Palettes and Subcategories with different unique names than pre-built will not be affected. Restarting the game is required afterwards." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RestoreDefaultPalettes)), "Re-install original pre-built Palettes and Subcategories? Any edits will be lost. This cannot be undone. Custom Palettes and Subcategories with different unique names than pre-built will not be affected. Restarting the game is required afterwards." },
            };
        }

        /// <summary>
        /// Returns the locale key for a warning tooltip.
        /// </summary>
        /// <param name="key">The bracketed portion of locale key.</param>
        /// <returns>Localization key for translations.</returns>
        public static string MouseTooltipKey(string key)
        {
            return $"{Mod.Id}.MOUSE_TOOLTIP[{key}]";
        }

        /// <summary>
        /// Returns the locale key for Palette or Subcategory prefab name.
        /// </summary>
        /// <param name="menuType">Palette or Subcategory.</param>
        /// <param name="uniqueName">Prefab name.</param>
        /// <returns>localization key.</returns>
        public static string NameKey(Systems.Palettes.PalettesUISystem.MenuType menuType, string uniqueName)
        {
            string prefix = Mod.Id + ".";
            prefix += menuType == Systems.Palettes.PalettesUISystem.MenuType.Palette ? nameof(Systems.Palettes.PalettesUISystem.MenuType.Palette) : nameof(Systems.Palettes.PalettesUISystem.MenuType.Subcategory);

            return $"{prefix}.NAME[{uniqueName}]";
        }

        /// <summary>
        /// Gets the locale key for Palette or subcategory prefab description.
        /// </summary>
        /// <param name="menuType">Palette or Subcategory.</param>
        /// <param name="uniqueName">Prefab name.</param>
        /// <returns>localization key.</returns>
        public static string DescriptionKey(Systems.Palettes.PalettesUISystem.MenuType menuType, string uniqueName)
        {
            string prefix = Mod.Id + ".";
            prefix += menuType == Systems.Palettes.PalettesUISystem.MenuType.Palette ? nameof(Systems.Palettes.PalettesUISystem.MenuType.Palette) : nameof(Systems.Palettes.PalettesUISystem.MenuType.Subcategory);

            return $"{prefix}.DESCRIPTION[{uniqueName}]";
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return m_Localization;
        }

        /// <inheritdoc/>
        public void Unload()
        {
        }

        private string TooltipDescriptionKey(string key)
        {
            return $"{Mod.Id}.TOOLTIP_DESCRIPTION[{key}]";
        }

        private string SectionLabel(string key)
        {
            return $"{Mod.Id}.SECTION_TITLE[{key}]";
        }

        private string TooltipTitleKey(string key)
        {
            return $"{Mod.Id}.TOOLTIP_TITLE[{key}]";
        }
    }
}
