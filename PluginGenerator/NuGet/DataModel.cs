//-----------------------------------------------------------------------
// <copyright file="DataModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarQube.Plugins
{
    [XmlRoot("package", Namespace = XmlNamespace)]
    public class NuGetPackage
    {
        public const string XmlNamespace = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd";

        [XmlElement("metadata")]
        public Metadata Metadata { get; set; }

        [XmlElement("files")]
        public NuGetFiles Files { get; set; }

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

            Serializer.SaveModel(this, fileName);
            this.FileName = fileName;
        }

        /// <summary>
        /// Loads and returns rules from the specified XML file
        /// </summary>
        public static NuGetPackage Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            NuGetPackage model = Serializer.LoadModel<NuGetPackage>(fileName);
            model.FileName = fileName;
            return model;
        }

        #endregion
    }

    public class Metadata
    {
        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("authors")]
        public string Authors { get; set; }

        [XmlElement("owners")]
        public string Owners { get; set; }

        [XmlElement("licenseUrl")]
        public string LicenseUrl { get; set; }

        [XmlElement("projectUrl")]
        public string ProjectUrl { get; set; }

        [XmlElement("iconUrl")]
        public string IconUrl { get; set; }

        [XmlElement("requireLicenseAcceptance")]
        public string RequireLicenseAcceptance { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("releaseNotes")]
        public string ReleaseNotes { get; set; }

        [XmlElement("copyright")]
        public string Copyright { get; set; }

        [XmlElement("tags")]
        public string Tags { get; set; }


    }

    public class NuGetFiles
    {
        [XmlElement("file")]
        public List<NuGetFile> Items { get; set; }
    }

    public class NuGetFile
    {
        [XmlAttribute("source")]
        public string Source { get; set; }

        [XmlAttribute("target")]
        public string Target { get; set; }

        [XmlAttribute("exclude")]
        public string Exclude { get; set; }

    }
}
