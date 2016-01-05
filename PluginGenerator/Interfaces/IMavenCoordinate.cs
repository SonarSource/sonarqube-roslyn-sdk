//-----------------------------------------------------------------------
// <copyright file="IMavenCoordinate.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarQube.Plugins.Maven
{
    /// <summary>
    /// Describes a specific version of a Maven artifact
    /// </summary>
    public interface IMavenCoordinate
    {
        string GroupId { get; }

        string ArtifactId { get; }

        string Version { get; }
    }
}
