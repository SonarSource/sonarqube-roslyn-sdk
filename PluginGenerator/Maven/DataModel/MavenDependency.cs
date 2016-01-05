//-----------------------------------------------------------------------
// <copyright file="MavenDependency.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Maven
{
    /// <summary>
    /// Describes a Maven dependency
    /// </summary>
    /// <remarks>This class is XML-serializable</remarks>
    public class MavenDependency : IMavenCoordinate
    {
        public MavenDependency()
        {
            this.Exclusions = new List<MavenCoordinate>();
        }

        public MavenDependency(string groupId, string artifactId, string version)
            : this()
        {
            if (string.IsNullOrWhiteSpace(groupId))
                { throw new ArgumentNullException("groupId"); }
            if (string.IsNullOrWhiteSpace(artifactId))
                { throw new ArgumentNullException("artifactId"); }
            if (string.IsNullOrWhiteSpace(version))
                { throw new ArgumentNullException("version"); }

            this.GroupId = groupId;
            this.ArtifactId = artifactId;
            this.Version = version;
        }

        [XmlElement("groupId")]
        public string GroupId { get; set; }

        [XmlElement("artifactId")]
        public string ArtifactId { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("scope")]
        public string Scope { get; set; }

        [XmlArray("exclusions")]
        [XmlArrayItem("exclusion")]
        public List<MavenCoordinate> Exclusions { get; set; }

        public override string ToString()
        {
            const string unspecified = "{null}";
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}:{1}:{2}",
                this.GroupId ?? unspecified,
                this.ArtifactId ?? unspecified,
                this.Version ?? unspecified);
        }

    }
}
