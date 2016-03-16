//-----------------------------------------------------------------------
// <copyright file="RoslynSdkConfiguration.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Roslyn
{
    public class RoslynSdkConfiguration
    {
        public RoslynSdkConfiguration()
        {
            this.Properties = new PluginProperties();
        }

        public string PluginKeyDifferentiator { get; set; }

        public string RepositoryKey { get; set; }

        public string RepositoryLanguage { get; set; }

        public string RepositoryName { get; set; }

        public string RulesXmlResourcePath { get; set; }

        public string SqaleXmlResourcePath { get; set; }

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
                throw new ArgumentNullException("fileName");
            }

            this.FileName = fileName;

            Serializer.SaveModel(this, fileName);
        }

        /// <summary>
        /// Loads and returns rules from the specified XML file
        /// </summary>
        public static RoslynSdkConfiguration Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            RoslynSdkConfiguration model = Serializer.LoadModel<RoslynSdkConfiguration>(fileName);
            model.FileName = fileName;
            return model;
        }

        #endregion Serialization
    }
}
