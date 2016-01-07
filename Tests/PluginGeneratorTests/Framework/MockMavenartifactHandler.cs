//-----------------------------------------------------------------------
// <copyright file="MockMavenArtifactHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Maven;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    public class MockMavenArtifactHandler : IMavenArtifactHandler
    {
        #region Test helpers

        public string JarToReturn { get; set; }

        #endregion

        #region IMavenArtifactHandler interface

        string IMavenArtifactHandler.FetchArtifactJarFile(IMavenCoordinate coordinate)
        {
            Assert.IsNotNull(coordinate);
            return this.JarToReturn;
        }

        #endregion
    }
}
