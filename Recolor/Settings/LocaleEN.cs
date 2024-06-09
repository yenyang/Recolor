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
                { SectionLabel("Channel0"), "Channel0" },
                { SectionLabel("Channel1"), "Channel1" },
                { SectionLabel("Channel2"), "Channel2" },
                { SectionLabel("Reset"), "Reset Colors" },
                { TooltipDescriptionKey("Reset"), "Resets and saves the colors back to the original colors for this asset (and season, if applicable)." },
                { SectionLabel("Save"), "Save Color Variation" },
                { TooltipDescriptionKey("Save"), "Saves a color variation for this asset (and season, if applicable) to an XML file located in a folder at %AppData%\\LocalLow\\Colossal Order\\Cities Skylines II \\ModsData\\Recolor \\ColorData\\ Triggers a color refresh on all assets of the same type." },
                { TooltipTitleKey("SingleInstance"), "Single Instance" },
                { TooltipDescriptionKey("SingleInstance"), "Change the colors of the current selection only. Does not work on plants and trees." },
                { TooltipTitleKey("Matching"), "Matching" },
                { TooltipDescriptionKey("Matching"), "Change the colors of all matching assets with same color variation (and season, if applicable). Single Instance will override this for that instance." },
            };
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
