// <copyright file="PaletteChooserUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>
namespace Recolor.Domain.Palette
{
    using Colossal.Entities;
    using Game.Prefabs;
    using Recolor.Domain.Palette.Prefabs;
    using System.Collections.Generic;
    using Unity.Entities;

    /// <summary>
    /// A class for handing dropdown items, subcategories, and palettes for palette chooser.
    /// </summary>
    public class PaletteChooserUIData
    {
        public Entity[][] m_DropdownItems;
        public Entity[] m_SelectedPaletteEntities;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteChooserUIData"/> class.
        /// </summary>
        public PaletteChooserUIData()
        {
            m_DropdownItems = new Entity[3][];
            m_SelectedPaletteEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteChooserUIData"/> class.
        /// </summary>
        /// <param name="keyValuePairs">Dictionary of subcategorys and palettes.</param>
        public PaletteChooserUIData(Dictionary<PaletteSubcategoryUIData, List<Entity>> keyValuePairs)
        {
            m_DropdownItems = new Entity[3][];
            for (int i = 0; i < 3; i++)
            {
                m_DropdownItems[i] = new Entity[CalculateLength(keyValuePairs)];
                int j = 0;
                foreach (KeyValuePair<PaletteSubcategoryUIData, List<Entity>> keyValuePair in keyValuePairs)
                {
                    m_DropdownItems[i][j++] = keyValuePair.Key.m_PrefabEntity;
                    for (int k = 0; k < keyValuePair.Value.Count; k++)
                    {
                        m_DropdownItems[i][j++] = keyValuePair.Value[k];
                    }
                }
            }

            m_SelectedPaletteEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteChooserUIData"/> class.
        /// </summary>
        /// <param name="keyValuePairs">Dictionary of subcategorys and palettes.</param>
        /// <param name="assignedPalettes">Buffer of Assigned palettes.</param>
        public PaletteChooserUIData(Dictionary<PaletteSubcategoryUIData, List<Entity>> keyValuePairs, DynamicBuffer<AssignedPalette> assignedPalettes)
        {
            m_DropdownItems = new Entity[3][];
            for (int i = 0; i < 3; i++)
            {
                m_DropdownItems[i] = new Entity[CalculateLength(keyValuePairs)];
                int j = 0;
                foreach (KeyValuePair<PaletteSubcategoryUIData, List<Entity>> keyValuePair in keyValuePairs)
                {
                    m_DropdownItems[i][j++] = keyValuePair.Key.m_PrefabEntity;
                    for (int k = 0; k < keyValuePair.Value.Count; k++)
                    {
                        m_DropdownItems[i][j++] = keyValuePair.Value[k];
                    }
                }
            }


            m_SelectedPaletteEntities = new Entity[3] { Entity.Null, Entity.Null, Entity.Null };
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();

            foreach (AssignedPalette assignedPalette in assignedPalettes)
            {
                if (assignedPalette.m_Channel >= 0 &&
                    assignedPalette.m_Channel <= 2 &&
                    prefabSystem.EntityManager.TryGetComponent(assignedPalette.m_PaletteInstanceEntity, out PrefabRef prefabRef) &&
                    prefabSystem.EntityManager.TryGetBuffer(prefabRef, isReadOnly: true, out DynamicBuffer<SwatchData> swatchDatas) &&
                    swatchDatas.Length >= 2 &&
                    prefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase) &&
                    prefabBase is PalettePrefab)
                {
                    m_SelectedPaletteEntities[assignedPalette.m_Channel] = prefabRef;
                }
            }
        }

        /// <summary>
        /// Gets or sets the dropdown items.
        /// </summary>
        public Entity[][] DropdownItems
        {
            get { return m_DropdownItems; }
            set { m_DropdownItems = value; }
        }

        /// <summary>
        /// Gets or sets the selectedIndexes.
        /// </summary>
        public Entity[] SelectedPaletteEntities
        {
            get { return m_SelectedPaletteEntities ; }
            set { m_SelectedPaletteEntities = value; }
        }

        /// <summary>
        /// Sets the prefab entity for a channel.
        /// </summary>
        /// <param name="channel">Channel 0 - 2.</param>
        /// <param name="prefabEntity">Palette Prefab Entity.</param>
        public void SetPrefabEntity(int channel, Entity prefabEntity)
        {
            if (channel >= 0 && channel <= 2)
            {
                m_SelectedPaletteEntities[channel] = prefabEntity;
            }
        }

        /// <summary>
        /// Gets the amount of chooseable palettes.
        /// </summary>
        /// <returns>Amount of chooseable palettes.</returns>
        public int GetPaletteCount()
        {
            int count = 0;
            for (int i = 0; i < m_DropdownItems.Length; i++)
            {
                count += m_DropdownItems.Length;
            }

            return count;
        }

        private int CalculateLength(Dictionary<PaletteSubcategoryUIData, List<Entity>> keyValuePairs)
        {
            int length = 0;
            foreach (KeyValuePair<PaletteSubcategoryUIData, List<Entity>> keyValuePair in keyValuePairs)
            {
                length++;
                length += keyValuePair.Value.Count;
            }

            return length;
        }

    }
}
