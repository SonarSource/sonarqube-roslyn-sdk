//-----------------------------------------------------------------------
// <copyright file="JarInfo.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarQube.Common;
using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarQube.Plugins.IntegrationTests
{
    [XmlRoot("JarInfo")]
    public class JarInfo
    {
        [XmlArray]
        [XmlArrayItem("Item")]
        public List<ManifestItem> Manifest { get; set; }

        [XmlArray("Extensions")]
        [XmlArrayItem("PropertyDefinition", Type = typeof(PropertyDefinition))]
        [XmlArrayItem("RulesDefinition", Type = typeof(RulesDefinition))]
        [XmlArrayItem("Unknown", Type = typeof(UnknownExtension))]
        public List<Extension> Extensions { get; set; }

        #region Serialization

        [XmlIgnore]
        public string FileName { get; private set; }

        /// <summary>
        /// Saves the project to the specified file as XML
        /// </summary>
        public void Save(string fileName, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            this.FileName = fileName;

            Serializer.SaveModel(this, fileName);
        }

        /// <summary>
        /// Loads and returns rules from the specified XML file
        /// </summary>
        public static JarInfo Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            JarInfo model = Serializer.LoadModel<JarInfo>(fileName);
            model.FileName = fileName;
            return model;
        }

        #endregion Serialization

        public class ManifestItem
        {
            [XmlAttribute("key")]
            public string Key { get; set; }
            [XmlAttribute("value")]
            public string Value { get; set; }
        }

        public abstract class Extension
        {
            [XmlAttribute("type")]
            public string Type { get; set; }
            [XmlAttribute("class")]
            public string Class { get; set; }
        }

        public class UnknownExtension : Extension
        {
        }

        public class PropertyDefinition : Extension
        {
            [XmlAttribute("key")]
            public string Key { get; set; }

            [XmlAttribute("defaultValue")]
            public string DefaultValue { get; set; }

        }

        public class RulesDefinition : Extension
        {
            public Repository Repository { get; set; }

        }

        public class Repository
        {
            [XmlAttribute("language")]
            public string Language { get; set; }

            [XmlAttribute("key")]
            public string Key { get; set; }

            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlArray("Rules")]
            [XmlArrayItem("Rule")]
            public List<Rule> Rules { get; set; }
        }

        public class Rule
        {
            [XmlAttribute("internalKey")]
            public string InternalKey { get; set; }

            [XmlAttribute("key")]
            public string Key { get; set; }

            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("severity")]
            public string Severity { get; set; }

        }
    }
}
