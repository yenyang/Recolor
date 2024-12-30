// <copyright file="GenericTooltipSystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Tools
{
    using System.Collections.Generic;
    using Colossal.Logging;
    using Game.Tools;
    using Game.UI.Localization;
    using Game.UI.Tooltip;
    using Recolor;

    /// <summary>
    /// Adds and removes tooltips.
    /// </summary>
    public partial class GenericTooltipSystem : TooltipSystemBase
    {
        private ToolSystem m_ToolSystem;
        private ILog m_Log;
        private Dictionary<string, StringTooltip> m_Tooltips;

        /// <summary>
        /// Registers a string tooltip to be displayed while using Subelement bulldozer tool.
        /// </summary>
        /// <param name="path">Unique string path for tooltip.</param>
        /// <param name="color">Tooltip color.</param>
        /// <param name="localeKey">Localization key.</param>
        /// <param name="fallback">Fallback string if localization key is not found.</param>
        /// <returns>True if tooltip added. False if already exists.</returns>
        public bool RegisterTooltip(string path, TooltipColor color, string localeKey, string fallback)
        {
            if (m_Tooltips.ContainsKey(path))
            {
                return false;
            }

            m_Log.Debug($"{nameof(GenericTooltipSystem)}.{nameof(RegisterTooltip)} Registering new tooltip {path}.");
            m_Tooltips.Add(path, new StringTooltip() { path = path, value = LocalizedString.IdWithFallback(localeKey, fallback), color = color });
            return true;
        }

        /// <summary>
        /// Adds an icon tooltip.
        /// </summary>
        /// <param name="path">Unique path.</param>
        /// <param name="icon">Icon path.</param>
        /// <returns>true if registered, false if path already exists.</returns>
        public bool RegisterIconTooltip(string path, string icon)
        {
            if (m_Tooltips.ContainsKey(path))
            {
                return false;
            }

            m_Log.Debug($"{nameof(GenericTooltipSystem)}.{nameof(RegisterTooltip)} Registering new icon tooltip {path}.");
            m_Tooltips.Add(path, new StringTooltip() { path = path, icon = icon });
            return true;
        }

        /// <summary>
        /// Removes a tooltip from registry if valid path.
        /// </summary>
        /// <param name="path">Unique string path for tooltip.</param>
        public void RemoveTooltip(string path)
        {
            if (m_Tooltips.ContainsKey(path))
            {
                m_Log.Debug($"{nameof(GenericTooltipSystem)}.{nameof(RemoveTooltip)} Removing tooltip {path}.");
                m_Tooltips.Remove(path);
            }
        }

        /// <summary>
        /// Removes all tooltips from registry.
        /// </summary>
        public void ClearTooltips()
        {
            m_Log.Debug($"{nameof(GenericTooltipSystem)}.{nameof(ClearTooltips)}");
            m_Tooltips.Clear();
        }


        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_Tooltips = new Dictionary<string, StringTooltip>();
            m_Log.Info($"{nameof(GenericTooltipSystem)} Created.");
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            foreach (StringTooltip stringTooltip in m_Tooltips.Values)
            {
                AddMouseTooltip(stringTooltip);
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
