// <copyright file="PalettesUISystem.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using Colossal.Logging;
    using Recolor.Domain.Palette;
    using Recolor.Extensions;

    /// <summary>
    /// A UI System for Palettes and Swatches.
    /// </summary>
    public partial class PalettesUISystem : ExtendedUISystemBase
    {
        private ValueBindingHelper<SwatchUIData[]> m_PaletteCreationMenuData;
        private ILog m_Log;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = Mod.Instance.Log;
            m_PaletteCreationMenuData = CreateBinding("PaletteCreationMenuData", new SwatchUIData[] { new SwatchUIData(UnityEngine.Color.white, 100, 0), new SwatchUIData(UnityEngine.Color.black, 100, 1) });

            m_Log.Info($"{nameof(PalettesUISystem)}.{nameof(OnCreate)}");
            Enabled = true;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}
