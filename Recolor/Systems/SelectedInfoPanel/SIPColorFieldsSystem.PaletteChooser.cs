// <copyright file="SIPColorFieldsSystem.PaletteChooser.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Systems.SelectedInfoPanel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Colossal.Serialization.Entities;
    using Colossal.UI.Binding;
    using Game;
    using Game.Common;
    using Game.Input;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Recolor.Domain;
    using Recolor.Domain.Palette;
    using Recolor.Extensions;
    using Recolor.Settings;
    using Recolor.Systems.ColorVariations;
    using Recolor.Systems.Palettes;
    using Recolor.Systems.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    /// Partial class for Selected Info panel mostly related to Palette choosing.
    /// </summary>
    public partial class SIPColorFieldsSystem : ExtendedInfoSectionBase
    {
        /// <summary>
        /// Updates the palettes binding.
        /// </summary>
        public void UpdatePalettes()
        {
            m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} starting");
            NativeArray<Entity> palettePrefabEntities = m_PaletteQuery.ToEntityArray(Allocator.Temp);

            Dictionary<string, List<PaletteUIData>> paletteChooserBuilder = new Dictionary<string, List<PaletteUIData>>();
            m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} palettePrefabs.length = {palettePrefabEntities.Length}.");
            paletteChooserBuilder.Add(NoSubcategoryName, new List<PaletteUIData>());
            foreach (Entity palettePrefabEntity in palettePrefabEntities)
            {
                if (!EntityManager.TryGetBuffer(palettePrefabEntity, isReadOnly: true, out DynamicBuffer<Swatch> swatches) ||
                    swatches.Length < 2)
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} skipping palette entity {palettePrefabEntity.Index}:{palettePrefabEntity.Version}.");
                    continue;
                }

                SwatchUIData[] swatchData = new SwatchUIData[swatches.Length];
                for (int i = 0; i < swatches.Length; i++)
                {
                    swatchData[i] = new SwatchUIData(swatches[i]);
                }

                if (!EntityManager.TryGetComponent(palettePrefabEntity, out PaletteSubcategoryData subcategoryData))
                {
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} doesn't have subcategorydata.");
                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} paletteChooserBuilder[NoSubcategoryName].count = {paletteChooserBuilder[NoSubcategoryName].Count}");
                    paletteChooserBuilder[NoSubcategoryName].Add(new PaletteUIData(palettePrefabEntity, swatchData));

                    m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} adding palette entity {palettePrefabEntity.Index}:{palettePrefabEntity.Version} with {swatches.Length} swatches.");
                }
            }

            m_PaletteChooserData.Value = new PaletteChooserUIData(paletteChooserBuilder);
            m_PaletteChooserData.Binding.TriggerUpdate();
            m_Log.Debug($"{nameof(PalettesUISystem)}.{nameof(UpdatePalettes)} complete");
        }

        private void AssignPaletteAction(int channel, Entity prefabEntity)
        {
            m_PaletteChooserData.Value.SetPrefabEntity(channel, prefabEntity);
            m_PaletteChooserData.Binding.TriggerUpdate();
            AssignPalette(channel, m_CurrentEntity, prefabEntity);
            m_PreviouslySelectedEntity = Entity.Null;
        }

        private void AssignPalette(int channel, Entity instanceEntity, Entity prefabEntity)
        {
            if (!EntityManager.HasBuffer<AssignedPalette>(instanceEntity))
            {
                EntityManager.AddBuffer<AssignedPalette>(instanceEntity);
            }

            DynamicBuffer<AssignedPalette> paletteAssignments = EntityManager.GetBuffer<AssignedPalette>(instanceEntity, isReadOnly: false);

            for (int i = 0; i < paletteAssignments.Length; i++)
            {
                if (paletteAssignments[i].m_Channel == channel)
                {
                    AssignedPalette paletteAssignment = paletteAssignments[i];
                    paletteAssignment.m_PaletteInstanceEntity = prefabEntity;
                    paletteAssignments[i] = paletteAssignment;
                    return;
                }
            }

            AssignedPalette newPaletteAssignment = new AssignedPalette()
            {
                m_Channel = channel,
                m_PaletteInstanceEntity = prefabEntity,
            };

            paletteAssignments.Add(newPaletteAssignment);
            EntityManager.AddComponent<BatchesUpdated>(instanceEntity);
            return;
        }
    }
}
