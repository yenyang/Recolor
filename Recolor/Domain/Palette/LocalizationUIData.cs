// <copyright file="LocalizationUIData.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Domain.Palette
{
    /// <summary>
    /// A class for handling localized names, descriptions and codes.
    /// </summary>
    public class LocalizationUIData
    {
        private string m_LocalizedName;
        private string m_LocalizedDescription;
        private string m_LocaleCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationUIData"/> class.
        /// </summary>
        public LocalizationUIData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationUIData"/> class.
        /// </summary>
        /// <param name="name">Localized name.</param>
        /// <param name="description">Localized description.</param>
        /// <param name="localeCode">Locale Code.</param>
        public LocalizationUIData(string localeCode, string name, string description)
        {
            m_LocalizedName = name;
            m_LocalizedDescription = description;
            m_LocaleCode = localeCode;
        }

        /// <summary>
        /// Gets or sets teh localized name.
        /// </summary>
        public string LocalizedName
        {
            get { return m_LocalizedName; }
            set { m_LocalizedName = value; }
        }

        /// <summary>
        /// Gets or sets the localized description.
        /// </summary>
        public string LocalizedDescription
        {
            get { return m_LocalizedDescription; }
            set { m_LocalizedDescription = value; }
        }

        /// <summary>
        /// Gets or sets the locale code.
        /// </summary>
        public string LocaleCode
        {
            get { return m_LocaleCode; }
            set { m_LocaleCode = value; }
        }
    }
}
