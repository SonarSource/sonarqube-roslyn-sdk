//-----------------------------------------------------------------------
// <copyright file="PluginManifest.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
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
        private readonly IDictionary<string, string> relativePathToFileMap;

        public PluginManifest()
        {
            this.relativePathToFileMap = new Dictionary<string, string>();
        }

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

            PluginManifest defn = null;
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
