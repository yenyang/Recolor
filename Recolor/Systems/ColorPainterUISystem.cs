// <copyright file="ColorPainterUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
{
    using Colossal.Logging;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Extensions;

    /// <summary>
    /// A UI System for the Color Painter Tool.
    /// </summary>
    public partial class ColorPainterUISystem : ExtendedUISystemBase
    {
        private ColorPainterToolSystem m_ColorPainterToolSystem;
        private ILog m_Log;
        private ValueBindingHelper<RecolorSet> m_PainterColorSet;
        private SelectedInfoPanelColorFieldsSystem m_SelectedInfoPanelColorFieldsSystem;
        private DefaultToolSystem m_DefaultToolSystem;
        private ToolSystem m_ToolSystem;
        private ValueBindingHelper<int> m_SelectionType;

        /// <summary>
        /// Used for different selection modes.
        /// </summary>
        public enum SelectionType
        {
            /// <summary>
            /// One Item at a time.
            /// </summary>
            Single,

            /// <summary>
            /// With a radius.
            /// </summary>
            Radius,
        }

        /// <summary>
        /// Gets the color set from UI.
        /// </summary>
        public ColorSet ColorSet
        {
            get { return m_PainterColorSet.Value.GetColorSet(); }
        }

        /// <summary>
        /// Gets the selection type for Color Painter tool.
        /// </summary>
        public SelectionType ColorPainterSelectionType
        {
            get { return (SelectionType)m_SelectionType.Value; }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(ColorPainterUISystem)}.{nameof(OnCreate)}");
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_ColorPainterToolSystem = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_SelectedInfoPanelColorFieldsSystem = World.GetOrCreateSystemManaged<SelectedInfoPanelColorFieldsSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

            // These establish bindings between UI and C#.
            m_PainterColorSet = CreateBinding("PainterColorSet", new RecolorSet(UnityEngine.Color.white, UnityEngine.Color.white, UnityEngine.Color.white));
            m_SelectionType = CreateBinding("ColorPainterSelectionType", (int)SelectionType.Single);

            // These are event triggers from actions in UI.
            CreateTrigger<int, UnityEngine.Color>("ChangePainterColor", ChangePainterColor);
            CreateTrigger("ColorPainterSingleSelection", () => m_SelectionType.Value = (int)SelectionType.Single);
            CreateTrigger("ColorPainterRadiusSelection", () => m_SelectionType.Value = (int)SelectionType.Radius);
            CreateTrigger("ColorPainterPasteColor", (int value) =>
            {
                m_Log.Debug(value);
                ChangePainterColor(value, m_SelectedInfoPanelColorFieldsSystem.CopiedColor);
                m_Log.Debug($"Copied Color: {m_SelectedInfoPanelColorFieldsSystem.CopiedColor.r},{m_SelectedInfoPanelColorFieldsSystem.CopiedColor.g},{m_SelectedInfoPanelColorFieldsSystem.CopiedColor.b}");
            });
            CreateTrigger("ColorPainterPasteColorSet", () =>
            {
                m_PainterColorSet.Value = new RecolorSet(m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet);
                m_Log.Debug("ColorPainterPasteColorSet");
                m_Log.Debug($"Channel0: {m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel0.r},{m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel0.g},{m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel0.b}");
                m_Log.Debug($"Channel1: {m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel1.r},{m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel1.g},{m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel1.b}");
                m_Log.Debug($"Channel2: {m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel2.r},{m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel2.g},{m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet.m_Channel2.b}");
            });
            CreateTrigger("ColorPainterCopyColorSet", () =>
            {
                m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet = m_PainterColorSet.Value.GetColorSet();
                m_SelectedInfoPanelColorFieldsSystem.CanPasteColorSet = true;
                m_Log.Debug("ColorPainterCopyColorSet");
                m_Log.Debug($"Channel0: {m_PainterColorSet.Value.Channel0.r},{m_PainterColorSet.Value.Channel0.g},{m_PainterColorSet.Value.Channel0.b}");
                m_Log.Debug($"Channel1: {m_PainterColorSet.Value.Channel1.r},{m_PainterColorSet.Value.Channel1.g},{m_PainterColorSet.Value.Channel1.b}");
                m_Log.Debug($"Channel2: {m_PainterColorSet.Value.Channel2.r},{m_PainterColorSet.Value.Channel2.g},{m_PainterColorSet.Value.Channel2.b}");
            });
            Enabled = false;
        }

        private void ChangePainterColor(int channel, UnityEngine.Color color)
        {
            m_Log.Debug(color);
            if (channel == 0)
            {
                m_PainterColorSet.Value.Channel0 = color;
            }
            else if (channel == 1)
            {
                m_PainterColorSet.Value.Channel1 = color;
            }
            else if (channel == 2)
            {
                m_PainterColorSet.Value.Channel2 = color;
            }
        }

    }
}
