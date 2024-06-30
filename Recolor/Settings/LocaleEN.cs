// <copyright file="LocaleEN.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Settings
{
    using System.Collections.Generic;
    using Colossal;
    using Recolor;

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
                { TooltipDescriptionKey("BuildingFilter"), "Toggling this off will prevent the Color Painter tool from chaning single instance colors of buildings." },
                { TooltipTitleKey("PropFilter"), "Prop Filter" },
                { TooltipDescriptionKey("PropFilter"), "Toggling this off will prevent the Color Painter tool from chaning single instance colors of props." },
                { TooltipTitleKey("VehicleFilter"), "Vehicle Filter" },
                { TooltipDescriptionKey("VehicleFilter"), "Toggling this off will prevent the Color Painter tool from chaning single instance colors of vehicles." },
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
