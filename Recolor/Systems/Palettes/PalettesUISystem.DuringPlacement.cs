// <copyright file="PalettesUISystem.DuringPlacement.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.Palettes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Colossal.Entities;
    using Colossal.Json;
    using Colossal.Localization;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Debug;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.SceneFlow;
    using Game.Tools;
    using Game.UI.InGame;
    using Newtonsoft.Json;
    using Recolor.Domain.Palette;
    using Recolor.Domain.Palette.Prefabs;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.SelectedInfoPanel;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Handles the localization portions for Palettes and Subcategories.
    /// </summary>
    public partial class PalettesUISystem : ExtendedUISystemBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether to ShowPaletteChooser during placement.
        /// </summary>
        public bool ShowPaletteChooserDuringPlacement
        {
            get { return m_ShowPaletteChooserDuringPlacement.Value; }
            set { m_ShowPaletteChooserDuringPlacement.Value = value; }
        }

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
        /// <param name="resetChoices">Reset choices back to three nones or not.</param>
        public void UpdatePaletteChoicesDuringPlacementBinding(bool resetChoices = false)
        {
            if ((m_ToolSystem.activeTool != m_ObjectToolSystem &&
                m_ToolSystem.activeTool != m_NetToolSystem) ||
               !m_PrefabSystem.TryGetEntity(m_ToolSystem.activePrefab, out Entity prefabEntity) ||
               !Mod.Instance.Settings.ShowPalettesOptionDuringPlacement ||
               EntityManager.HasComponent<Game.Prefabs.CreatureData>(prefabEntity))
            {
                m_ShowPaletteChooserDuringPlacement.Value = false;
                return;
            }

            if (resetChoices == false &&
                Mod.Instance.Settings.PaletteChooserBehaviorWhenSwitchingPrefab == Setting.PaletteChooserBehavior.RememberPrevious)
            {
                if (m_PalettePreferenceSystem.TryGetPalettePrefabPreference(prefabEntity, out Entity[] newPalettePrefabEntities))
                {
                    m_PaletteChoicesDuringPlacementDatas.Value.SelectedPaletteEntities = newPalettePrefabEntities;
                }
                else
                {
                    resetChoices = true;
                }
            }

            m_SIPColorFieldsSystem.UpdatePalettes(prefabEntity, ref m_PaletteChoicesDuringPlacementDatas, resetChoices);
            m_ShowPaletteChooserDuringPlacement.Value = m_PaletteChoicesDuringPlacementDatas.Value.GetPaletteCount() > 0 ? true : false;
        }

        /// <summary>
        /// Sets the none palette colors.
        /// </summary>
        /// <param name="colorSet">Set of three colors to set none palette colors to.</param>
        public void SetNoneColors(ColorSet colorSet)
        {
            if (m_NonePaletteColors.Value[0] == colorSet.m_Channel0 &&
                m_NonePaletteColors.Value[1] == colorSet.m_Channel1 &&
                m_NonePaletteColors.Value[2] == colorSet.m_Channel2)
            {
                return;
            }

            m_NonePaletteColors.Value = new Color[] { colorSet.m_Channel0, colorSet.m_Channel1, colorSet.m_Channel2 };
            m_NonePaletteColors.Binding.TriggerUpdate();
        }

        /// <summary>
        /// Resets the none palette colors.
        /// </summary>
        public void ResetNoneColors()
        {
            m_NonePaletteColors.Value = new Color[] { DefaultNoneColor, DefaultNoneColor, DefaultNoneColor };
        }

        private void AssignPaletteDuringPlacementAction(int channel, Entity palettePrefabEntity)
        {
            m_PaletteChoicesDuringPlacementDatas.Value.SetPrefabEntity(channel, palettePrefabEntity);
            m_PaletteChoicesDuringPlacementDatas.Binding.TriggerUpdate();
            if (palettePrefabEntity == Entity.Null &&
                m_NonePaletteColors.Value.Length > channel)
            {
                m_NonePaletteColors.Value[channel] = DefaultNoneColor;
            }

            if (m_PrefabSystem.TryGetEntity(m_ToolSystem.activePrefab, out Entity prefabEntity) &&
                Mod.Instance.Settings.PaletteChooserBehaviorWhenSwitchingPrefab == Setting.PaletteChooserBehavior.RememberPrevious)
            {
                m_PalettePreferenceSystem.RegisterPalettePreference(prefabEntity, m_PaletteChoicesDuringPlacementDatas.Value.SelectedPaletteEntities);
            }
        }

        private void RemovePaletteDuringPlacementAction(int channel)
        {
            AssignPaletteDuringPlacementAction(channel, Entity.Null);
        }

        private void MinimizePalettesDuringPlacement()
        {
            m_MinimizePaletteChooserDuringPlacement.Value = true;
            Mod.Instance.Settings.MinimizePaletteChooserDuringPlacement = true;
            Mod.Instance.Settings.ApplyAndSave();
        }

        private void MaximizePalettesDuringPlacement()
        {
            m_MinimizePaletteChooserDuringPlacement.Value = false;
            Mod.Instance.Settings.MinimizePaletteChooserDuringPlacement = false;
            Mod.Instance.Settings.ApplyAndSave();
        }

        private void HidePalettesDuringPlacement()
        {
            m_ShowPaletteChooserDuringPlacement.Value = false;
            Mod.Instance.Settings.ShowPalettesOptionDuringPlacement = false;
            Mod.Instance.Settings.ApplyAndSave();
        }
    }
}
