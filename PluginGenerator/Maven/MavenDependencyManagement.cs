//-----------------------------------------------------------------------
// <copyright file="MavenDependencyManagement.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Maven
{
    /// <summary>
    /// Describes the Maven dependency management element in the POM
    /// </summary>
    /// <remarks>This class is XML-serializable</remarks>
    public class MavenDependencyManagement
    {
        public MavenDependencyManagement()
        {
            this.Dependencies = new List<MavenDependency>();
        }

        [XmlArray("dependencies")]
        [XmlArrayItem("dependency")]
        public List<MavenDependency> Dependencies { get; set; }
    }
}
