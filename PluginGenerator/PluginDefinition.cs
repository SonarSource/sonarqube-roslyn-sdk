//-----------------------------------------------------------------------
// <copyright file="PluginDefinition.cs" company="SonarSource SA and Microsoft Corporation">
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
    public class PluginDefinition
    {
        private readonly IDictionary<string, string> relativePathToFileMap;

        public PluginDefinition()
        {
            this.relativePathToFileMap = new Dictionary<string, string>();
        }

        public string Language { get; set; }

        public string Key { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string License { get; set; }
        public string OrganizationUrl { get; set; }
        public string Homepage { get; set; }
        public string Class { get; set; }
        public string SourcesUrl { get; set; }
        public string Developers { get; set; }
        public string IssueTrackerUrl { get; set; }
        public string TermsConditionsUrl { get; set; }
        public string Organization { get; set; }

        /// <summary>
        /// Returns additional files that should be added to the jar.
        /// Key: relative path in jar
        /// Value: the full path to the source file
        /// </summary>
        public IDictionary<string, string> AdditionalFileMap { get { return this.relativePathToFileMap; } }

        /// <summary>
        /// Additional source files that should be compiled as part of the jar
        /// </summary>
        public IList<string> AdditionalSourceFiles { get; set; }

        /// <summary>
        /// The fully-qualified names of any additional compied classes that should
        /// be exported as SonarQube extensions
        /// </summary>
        public IList<string> AdditionalExtensions { get; set; }

        #region Serialization

        [XmlIgnore]
        public string FilePath { get; private set; }

        public static PluginDefinition Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(PluginDefinition));

            PluginDefinition defn = null;
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                defn = serializer.Deserialize(stream) as PluginDefinition;
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

            XmlSerializer serializer = new XmlSerializer(typeof(PluginDefinition));

            using (Stream stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, this);
            }
            this.FilePath = FilePath;
        }

        #endregion
    }
}
