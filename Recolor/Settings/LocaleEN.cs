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
                { TooltipTitleKey("SingleInstance"), "Single Instance" },
                { TooltipDescriptionKey("SingleInstance"), "Change the colors of the current selection only." },
                { TooltipTitleKey("Matching"), "Matching" },
                { TooltipDescriptionKey("Matching"), "Change the colors of all matching assets with same color variation (and season, if applicable). Single Instance will override this for that instance." },
                { SectionLabel("Selection"), "Selection" },
                { TooltipTitleKey("SingleSelection"), "Single" },
                { TooltipDescriptionKey("SingleSelection"), "Changes the single instance colors of one buildings, prop, or vehicle." },
                { TooltipTitleKey("RadiusSelection"), "Radius Selection" },
                { TooltipDescriptionKey("RadiusSelection"), "Changes the single isntance colors of buildings, prop, and/or vehicles within the radius." },
                { TooltipDescriptionKey("IncreaseRadius"), "Increase the radius." },
                { TooltipDescriptionKey("DecreaseRadius"), "Decrease the radius." },
                { SectionLabel("Filter"), "Filter" },
                { TooltipTitleKey("AllFilters"), "Toggle all Filters on/off" },
                { TooltipDescriptionKey("AllFilters"), "Either selects all or none of the Filters depending on your current selection. Having none selected will prevent the Bulldoze Tool from working." },
                { TooltipTitleKey("BuildingFilter"), "Building Filter" },
                { TooltipDescriptionKey("BuildingFilter"), "Toggling this off will prevent the Color Painter tool from chaning single instance colors of buildings." },
                { TooltipTitleKey("PropFilter"), "Prop Filter" },
                { TooltipDescriptionKey("PropFilter"), "Toggling this off will prevent the Color Painter tool from chaning single instance colors of props." },
                { TooltipTitleKey("VehicleFilter"), "Vehicle Filter" },
                { TooltipDescriptionKey("PropFilter"), "Toggling this off will prevent the Color Painter tool from chaning single instance colors of vehicles." },
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
