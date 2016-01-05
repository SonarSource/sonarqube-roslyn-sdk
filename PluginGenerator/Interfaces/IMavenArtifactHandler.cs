//-----------------------------------------------------------------------
// <copyright file="IMavenArtifactHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;

namespace SonarQube.Plugins.Maven
{
    public interface IMavenArtifactHandler
    {
        /// <summary>
        /// Returns a local path to the jar of the specified Maven artifact.
        /// </summary>
        /// <param name="coordinate">Identifier for a Maven artifact</param>
        /// <returns>Local file paths to the jar</returns>
        string FetchArtifactJarFile(IMavenCoordinate coordinate);
    }
}
