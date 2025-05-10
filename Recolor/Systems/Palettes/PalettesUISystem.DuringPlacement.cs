// <copyright file="PalettesUISystem.Localization.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Colossal.Entities;
    using Colossal.IO.AssetDatabase;
    using Colossal.Json;
    using Colossal.Localization;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Game.Tools;
    using Game.UI.InGame;
    using Newtonsoft.Json;
    using Recolor.Domain.Palette;
    using Recolor.Domain.Palette.Prefabs;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.SelectedInfoPanel;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Handles the localization portions for Palettes and Subcategories.
    /// </summary>
    public partial class PalettesUISystem : ExtendedUISystemBase
    {
        /// <summary>
        /// Gets the selected palettes during placement.
        /// </summary>
        public Entity[] SelectedPalettesDuringPlacement
        {
            get { return m_PaletteChoicesDuringPlacementDatas.Value.m_SelectedPaletteEntities; }
        }

        /// <summary>
        /// Updates Palette choices during placement binding.
        /// </summary>
        public void UpdatePaletteChoicesDuringPlacementBinding()
        {
            if (m_ToolSystem.activeTool != m_ObjectToolSystem ||
               !m_PrefabSystem.TryGetEntity(m_ToolSystem.activePrefab, out Entity prefabEntity))
            {
                return;
            }

            m_SIPColorFieldsSystem.UpdatePalettes(prefabEntity, ref m_PaletteChoicesDuringPlacementDatas);
        }

        private void AssignPaletteDuringPlacementAction(int channel, Entity prefabEntity)
        {
            m_PaletteChoicesDuringPlacementDatas.Value.SetPrefabEntity(channel, prefabEntity);
            m_PaletteChoicesDuringPlacementDatas.Binding.TriggerUpdate();
        }

        private void RemovePaletteDuringPlacementAction(int channel)
        {
            AssignPaletteDuringPlacementAction(channel, Entity.Null);
        }
    }
}
