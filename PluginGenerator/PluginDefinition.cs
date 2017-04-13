/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
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
using System.IO;
using System.Xml.Serialization;

namespace SonarQube.Plugins
{
    /// <summary>
    /// Defines properties that appear in the jar manifest file and that provide
    /// metadata to SonarQube about the plugin.
    /// </summary>
    public class PluginManifest
    {
        /// <summary>
        /// Provide a unique identifier for the plugin
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The plugin version. Used by the "Update Centre" in SonarQube to
        /// determine whether upgrades are available.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Identifies the entry point for the plugin i.e. the class that
        /// inherits from "Plugin" that tells SonarQube which exports the
        /// plugin provides
        /// </summary>
        public string Class { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string License { get; set; }
        public string OrganizationUrl { get; set; }
        public string Homepage { get; set; }
        public string SourcesUrl { get; set; }
        public string Developers { get; set; }
        public string IssueTrackerUrl { get; set; }
        public string TermsConditionsUrl { get; set; }
        public string Organization { get; set; }

        #region Serialization

        [XmlIgnore]
        public string FilePath { get; private set; }

        public static PluginManifest Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(PluginManifest));

            PluginManifest defn;
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                defn = serializer.Deserialize(stream) as PluginManifest;
            }
            defn.FilePath = filePath;
            return defn;
        }

        public void Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(PluginManifest));

            using (Stream stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, this);
            }
            this.FilePath = FilePath;
        }

        #endregion
    }
}
