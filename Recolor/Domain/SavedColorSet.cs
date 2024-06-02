// <copyright file="SavedColorSet.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain
{
    using Game.Prefabs;
    using Game.Rendering;
    using Recolor.Systems;

    /// <summary>
    /// A class to use for XML Serialization and deserialization of custom foliage color sets.
    /// </summary>
    public class SavedColorSet
    {
        private ColorSet m_ColorSet;
        private string m_PrefabType;
        private string m_PrefabName;
        private int m_Index;
        private int m_Version;

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedColorSet"/> class.
        /// </summary>
        public SavedColorSet()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedColorSet"/> class.
        /// </summary>
        /// <param name="channel0">One color.</param>
        /// <param name="channel1">2nd color.</param>
        /// <param name="channel2">3rd color.</param>
        /// <param name="prefabID">The prefab this applies to.</param>
        /// <param name="season">season this applies to.</param>
        /// <param name="index">index for color variation.</param>
        public SavedColorSet(UnityEngine.Color channel0, UnityEngine.Color channel1, UnityEngine.Color channel2, PrefabID prefabID, int index)
        {
            m_ColorSet = default;
            m_ColorSet.m_Channel0 = channel0;
            m_ColorSet.m_Channel1 = channel1;
            m_ColorSet.m_Channel2 = channel2;
            m_PrefabName = prefabID.GetName();
            m_PrefabType = prefabID.ToString().Remove(prefabID.ToString().IndexOf(':'));
            m_Version = 1;
            m_Index = index;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedColorSet"/> class.
        /// </summary>
        /// <param name="colorSet">set of colors</param>
        /// <param name="prefabID">The prefab this applies to.</param>
        /// <param name="index">index for color variation.</param>
        public SavedColorSet(ColorSet colorSet, PrefabID prefabID, int index)
        {
            m_ColorSet = colorSet;
            m_PrefabName = prefabID.GetName();
            m_PrefabType = prefabID.ToString().Remove(prefabID.ToString().IndexOf(':'));
            m_Version = 1;
            m_Index = index;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedColorSet"/> class.
        /// </summary>
        /// <param name="colorSet">set of colors</param>
        /// <param name="assetSeasonIdentifier">struct with required data.</param>
        public SavedColorSet(ColorSet colorSet, SelectedInfoPanelColorFieldsSystem.AssetSeasonIdentifier assetSeasonIdentifier)
        {
            m_ColorSet = colorSet;
            m_PrefabName = assetSeasonIdentifier.m_PrefabID.GetName();
            m_PrefabType = assetSeasonIdentifier.m_PrefabID.ToString().Remove(assetSeasonIdentifier.m_PrefabID.ToString().IndexOf(':'));
            m_Version = 1;
            m_Index = assetSeasonIdentifier.m_Index;
        }

        /// <summary>
        /// Gets or sets a value for the colorset.
        /// </summary>
        public ColorSet ColorSet
        {
            get { return m_ColorSet; }
            set { m_ColorSet = value; }
        }

        /// <summary>
        /// Gets or sets a value for the prefab type.
        /// </summary>
        public string PrefabType
        {
            get { return m_PrefabType; }
            set { m_PrefabType = value; }
        }

        /// <summary>
        /// Gets or sets a value for the prefab name.
        /// </summary>
        public string PrefabName
        {
            get { return m_PrefabName; }
            set { m_PrefabName = value; }
        }

        /// <summary>
        /// Gets or sets a value for the version.
        /// </summary>
        public int Index
        {
            get { return m_Index; }
            set { m_Index = value; }
        }

        /// <summary>
        /// Gets or sets a value for the version.
        /// </summary>
        public int Version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }

        /// <summary>
        /// Sets Color set based on 3 colors.
        /// </summary>
        /// <param name="channel0">One color.</param>
        /// <param name="channel1">2nd color.</param>
        /// <param name="channel2">3rd color.</param>
        public void SetColorSet(UnityEngine.Color channel0, UnityEngine.Color channel1, UnityEngine.Color channel2)
        {
            m_ColorSet.m_Channel0 = channel0;
            m_ColorSet.m_Channel1 = channel1;
            m_ColorSet.m_Channel2 = channel2;
        }
    }
}
