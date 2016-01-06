//-----------------------------------------------------------------------
// <copyright file="MavenArtifactHandlerExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SonarQube.Plugins.Maven
{
    /// <summary>
    /// Extension methods for IMavenArtifactHandler
    /// </summary>
    public static class MavenArtifactHandlerExtensions
    {
        public static MavenPartialPOM GetPOMFromResource(this IMavenArtifactHandler handler, Assembly resourceAssembly, string resourceName)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException("resourceAssembly");
            }
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentNullException("resourceName");
            }

            MavenPartialPOM pom = null;
            using (Stream stream = resourceAssembly.GetManifestResourceStream(resourceName))
            {
                pom = MavenPartialPOM.Load(stream);
            }
            return pom;
        }

        public static IEnumerable<string> GetJarsFromPOM(this IMavenArtifactHandler handler, MavenPartialPOM pom)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (pom == null)
            {
                throw new ArgumentNullException("pom");
            }

            IList<string> jarFilePaths = new List<string>();

            foreach (MavenDependency dependency in pom.Dependencies)
            {
                string jarFilePath = handler.FetchArtifactJarFile(dependency);
                if (jarFilePath != null)
                {
                    jarFilePaths.Add(jarFilePath);
                }
            }
            return jarFilePaths;
        }

    }
}
