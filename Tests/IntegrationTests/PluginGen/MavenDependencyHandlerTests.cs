//-----------------------------------------------------------------------
// <copyright file="MavenDependencyHandlerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Maven;
using SonarQube.Plugins.Test.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonarQube.Plugins.IntegrationTests
{
    [TestClass]
    public class MavenDependencyHandlerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [TestCategory("Integration")]
        public void MavenDownload_SonarApiPluginWithoutDependencies_Succeeds()
        {
            // Arrange
            string localMavenDir = TestUtils.CreateTestDirectory(this.TestContext, ".localMaven");
            MavenCoordinate coordinate = new MavenCoordinate()
            {
                GroupId = "org.codehaus.sonar",
                ArtifactId = "sonar-plugin-api",
                Version = "4.5.2"
            };

            TestLogger logger = new TestLogger();
            MavenDependencyHandler subject = new MavenDependencyHandler(localMavenDir, logger);

            // Act
            IEnumerable<string> result = subject.GetJarFiles(coordinate, false);

            // Arrange
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(File.Exists(result.Single()));

            Assert.IsTrue(result.All(r => Path.IsPathRooted(r)));
            Assert.IsTrue(result.All(r => r.EndsWith(".jar")));

            Assert.AreEqual(localMavenDir, subject.LocalCacheDirectory);
            Assert.IsTrue(Directory.Exists(localMavenDir));

            logger.AssertErrorsLogged(0);
            logger.AssertWarningsLogged(0);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void MavenDownload_SonarApiPluginWithDependencies_Succeeds()
        {
            // Arrange
            string localMavenDir = TestUtils.CreateTestDirectory(this.TestContext, ".localMaven");
            MavenCoordinate coordinate = new MavenCoordinate()
            {
                GroupId = "org.codehaus.sonar",
                ArtifactId = "sonar-plugin-api",
                Version = "4.5.2"
            };
            coordinate = new MavenCoordinate()
            {
                GroupId = "com.google.guava",
                ArtifactId = "guava",
                Version = "10.0.1"
            };

            TestLogger logger = new TestLogger();
            MavenDependencyHandler subject = new MavenDependencyHandler(localMavenDir, logger);

            // Act
            IEnumerable<string> result = subject.GetJarFiles(coordinate, true);

            // Arrange
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() > 1, "Expecting multiple jars to have been downloaded");

            Assert.IsTrue(result.All(r => Path.IsPathRooted(r)));
            Assert.IsTrue(result.All(r => File.Exists(r)));
            Assert.IsTrue(result.All(r => r.EndsWith(".jar")));

            logger.AssertErrorsLogged(0);
//            logger.AssertWarningsLogged(0);
        }
    }
}
