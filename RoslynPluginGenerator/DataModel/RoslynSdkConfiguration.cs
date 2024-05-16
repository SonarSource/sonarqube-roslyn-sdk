/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Roslyn
{
    public class RoslynSdkConfiguration
    {
        public RoslynSdkConfiguration()
        {
            Properties = new PluginProperties();
        }

        public string PluginKeyDifferentiator { get; set; }

        public string RepositoryKey { get; set; }

        public string RepositoryLanguage { get; set; }

        public string RepositoryName { get; set; }

        public string RulesXmlResourcePath { get; set; }

        [XmlElement("PluginProperties")]
        public PluginProperties Properties { get; set; }

        #region Serialization

        [XmlIgnore]
        public string FileName { get; private set; }

        /// <summary>
        /// Saves the project to the specified file as XML
        /// </summary>
        public void Save(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            FileName = fileName;

            Serializer.SaveModel(this, fileName);
        }

        /// <summary>
        /// Loads and returns rules from the specified XML file
        /// </summary>
        public static RoslynSdkConfiguration Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            RoslynSdkConfiguration model = Serializer.LoadModel<RoslynSdkConfiguration>(fileName);
            model.FileName = fileName;
            return model;
        }

        #endregion Serialization
    }
}