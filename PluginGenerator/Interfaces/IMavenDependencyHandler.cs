//-----------------------------------------------------------------------
// <copyright file="IMavenDependencyHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;

namespace SonarQube.Plugins.Maven
{
    public interface IMavenDependencyHandler
    {
        /// <summary>
        /// Returns a local path to the jar of the specified Maven artifact. Optionally
        /// includes all of the jars required by the specified artifact.
        /// </summary>
        /// <param name="coordinate">Identifier for a Maven artifact</param>
        /// <param name="includeDependencies">True if dependencies of the artifact should be included</param>
        /// <returns>Local file paths to the jars</returns>
        IEnumerable<string> GetJarFiles(MavenCoordinate coordinate, bool includeDependencies);
    }
}
