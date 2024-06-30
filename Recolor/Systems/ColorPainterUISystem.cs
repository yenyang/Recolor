// <copyright file="ColorPainterUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems
{
    using Colossal.Logging;
    using Game.Input;
    using Game.Rendering;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Extensions;
    using Recolor.Settings;

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
        private ValueBindingHelper<RecolorSet> m_CopiedColorSet;
        private ValueBindingHelper<UnityEngine.Color> m_CopiedColor;
        private ValueBindingHelper<int> m_Radius;
        private ValueBindingHelper<int> m_Filter;
        private ValueBindingHelper<PainterToolMode> m_ToolMode;

        /// <summary>
        /// Used for determining the mode of the painter tool.
        /// </summary>
        public enum PainterToolMode
        {
            /// <summary>
            /// Change colors.
            /// </summary>
            Paint,

            /// <summary>
            /// Reset back to vanilla.
            /// </summary>
            Reset,

            /// <summary>
            /// Pick a new color.
            /// </summary>
            Picker,
        }

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
        /// Use to filter selection with radius.
        /// </summary>
        public enum FilterType
        {
            /// <summary>
            /// Just buildings.
            /// </summary>
            Building,

            /// <summary>
            /// Props but not trees or plants.
            /// </summary>
            Props,

            /// <summary>
            /// Vehicles.
            /// </summary>
            Vehicles,
        }

        /// <summary>
        /// Gets the tool mode for color painter.
        /// </summary>
        public PainterToolMode ToolMode
        {
            get { return m_ToolMode; }
        }

        /// <summary>
        /// Gets or sets the color set for color painter.
        /// </summary>
        public ColorSet ColorSet
        {
            get { return m_PainterColorSet.Value.GetColorSet(); }
            set { m_PainterColorSet.Value = new RecolorSet(value); }
        }

        /// <summary>
        /// Gets the selection type for Color Painter tool.
        /// </summary>
        public SelectionType ColorPainterSelectionType
        {
            get { return (SelectionType)m_SelectionType.Value; }
        }

        /// <summary>
        /// Gets the filter type for Color Painter Tool.
        /// </summary>
        public FilterType ColorPainterFilterType
        {
            get { return (FilterType)m_Filter.Value; }
        }

        /// <summary>
        /// Gets the value of the Radius binding.
        /// </summary>
        public int Radius
        {
            get { return m_Radius.Value; }
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
            m_ToolSystem.EventToolChanged += OnToolChanged;

            // These establish bindings between UI and C#.
            m_PainterColorSet = CreateBinding("PainterColorSet", new RecolorSet(UnityEngine.Color.white, UnityEngine.Color.white, UnityEngine.Color.white));
            m_SelectionType = CreateBinding("ColorPainterSelectionType", (int)SelectionType.Single);
            m_CopiedColorSet = CreateBinding("CopiedColorSet", new RecolorSet(UnityEngine.Color.white, UnityEngine.Color.white, UnityEngine.Color.white));
            m_CopiedColor = CreateBinding("CopiedColor", UnityEngine.Color.white);
            m_Radius = CreateBinding("Radius", 30);
            m_Filter = CreateBinding("Filter", (int)FilterType.Building);
            m_ToolMode = CreateBinding("PainterToolMode", PainterToolMode.Paint);

            // These are event triggers from actions in UI.
            CreateTrigger<int, UnityEngine.Color>("ChangePainterColor", ChangePainterColor);
            CreateTrigger("ColorPainterSingleSelection", () => m_SelectionType.Value = (int)SelectionType.Single);
            CreateTrigger("ColorPainterRadiusSelection", () => m_SelectionType.Value = (int)SelectionType.Radius);
            CreateTrigger("CopyColor", (UnityEngine.Color color) => m_CopiedColor.Value = color);
            CreateTrigger("ColorPainterPasteColor", (int value) => ChangePainterColor(value, m_SelectedInfoPanelColorFieldsSystem.CopiedColor));
            CreateTrigger("ColorPainterPasteColorSet", () => m_PainterColorSet.Value = new RecolorSet(m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet));
            CreateTrigger("ColorPainterCopyColorSet", () =>
            {
                m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet = m_PainterColorSet.Value.GetColorSet();
                m_SelectedInfoPanelColorFieldsSystem.CanPasteColorSet = true;
                m_CopiedColorSet.Value = new RecolorSet(m_PainterColorSet.Value.GetColorSet());
            });
            CreateTrigger("IncreaseRadius", IncreaseRadius);
            CreateTrigger("DecreaseRadius", DecreaseRadius);
            CreateTrigger("BuildingFilter", () => m_Filter.Value = (int)FilterType.Building);
            CreateTrigger("PropFilter", () => m_Filter.Value = (int)FilterType.Props);
            CreateTrigger("VehicleFilter", () => m_Filter.Value = (int)FilterType.Vehicles);
            CreateTrigger("ChangeToolMode", (int toolMode) => m_ToolMode.Value = (PainterToolMode)toolMode);

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

        private void OnToolChanged(ToolBaseSystem tool)
        {
            if (tool == m_ColorPainterToolSystem)
            {
                m_CopiedColorSet.Value = new RecolorSet(m_SelectedInfoPanelColorFieldsSystem.CopiedColorSet);
                m_CopiedColor.Value = m_SelectedInfoPanelColorFieldsSystem.CopiedColor;
            }
        }

        private void IncreaseRadius()
        {
            if (m_Radius.Value < 10)
            {
                m_Radius.Value += 1;
            }
            else if (m_Radius.Value < 100)
            {
                m_Radius.Value += 10;
            }
            else if (m_Radius.Value < 1000)
            {
                m_Radius.Value += 100;
            }
        }

        private void DecreaseRadius()
        {
            if (m_Radius.Value > 100)
            {
                m_Radius.Value -= 100;
            }
            else if (m_Radius.Value > 10)
            {
                m_Radius.Value -= 10;
            }
            else if (m_Radius.Value > 1)
            {
                m_Radius.Value -= 1;
            }
        }
    }
}
