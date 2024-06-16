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
        private DefaultToolSystem m_DefaultToolSystem;
        private ToolSystem m_ToolSystem;

        /// <summary>
        /// Gets the color set from UI.
        /// </summary>
        public ColorSet ColorSet
        {
            get { return m_PainterColorSet.Value.GetColorSet(); }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_Log.Info($"{nameof(ColorPainterUISystem)}.{nameof(OnCreate)}");
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_ColorPainterToolSystem = World.GetOrCreateSystemManaged<ColorPainterToolSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_PainterColorSet = CreateBinding("PainterColorSet", new RecolorSet(UnityEngine.Color.white, UnityEngine.Color.white, UnityEngine.Color.white));
            CreateTrigger<int, UnityEngine.Color>("ChangePainterColor", ChangePainterColor);
            Enabled = false;
        }

        private void ChangePainterColor(int channel, UnityEngine.Color color)
        {
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
