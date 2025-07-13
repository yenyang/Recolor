// <copyright file="PaletteFilterEntityUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    using Game.Prefabs;
    using Game.UI;
    using Unity.Entities;

    /// <summary>
    /// UI data for palete filter entities.
    /// </summary>
    public class PaletteFilterEntityUIData
    {
        private Entity m_FilterPrefabEntity;
        private string m_LocaleKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteFilterEntityUIData"/> class.
        /// </summary>
        public PaletteFilterEntityUIData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaletteFilterEntityUIData"/> class.
        /// </summary>
        /// <param name="prefabEntity">prefab entity for filter.</param>
        /// <param name="localeKey">Key for localized text.</param>
        public PaletteFilterEntityUIData(Entity prefabEntity, string localeKey)
        {
            m_FilterPrefabEntity = prefabEntity;
            m_LocaleKey = localeKey;
        }

        /// <summary>
        /// Gets or sets a value for the Locale Key.
        /// </summary>
        public string LocaleKey
        {
            get { return m_LocaleKey; }
            set { m_LocaleKey = value; }
        }

        /// <summary>
        /// Gets or sets a value for the filter prefab entity.
        /// </summary>
        public Entity FilterPrefabEntity
        {
            get { return m_FilterPrefabEntity; }
            set { m_FilterPrefabEntity = value; }
        }

        /// <summary>
        /// Gets a value for the image src.
        /// </summary>
        public string Src
        {
            get { return GetThumbnailOrPlaceholder(); }
        }

        private string GetThumbnailOrPlaceholder()
        {
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            ImageSystem imageSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ImageSystem>();
            if (prefabSystem.TryGetPrefab(m_FilterPrefabEntity, out PrefabBase prefabBase))
            {
                return imageSystem.GetThumbnail(m_FilterPrefabEntity);
            }
            else
            {
                return imageSystem.placeholderIcon;
            }
        }

    }
}
