//-----------------------------------------------------------------------
// <copyright file="MavenDependency.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Maven
{
    /// <summary>
    /// Describes a Maven dependency
    /// </summary>
    /// <remarks>This class is XML-serializable</remarks>
    public class MavenDependency : MavenCoordinate
    {
        public MavenDependency()
        {
            this.Exclusions = new List<MavenCoordinate>();
        }

        public MavenDependency(string groupId, string artifactId, string version)
            : base(groupId, artifactId, version)
        {
            this.Exclusions = new List<MavenCoordinate>();
        }

        [XmlElement("scope")]
        public string Scope { get; set; }

        [XmlArray("exclusions")]
        [XmlArrayItem("exclusion")]
        public List<MavenCoordinate> Exclusions { get; set; }
    }
}
