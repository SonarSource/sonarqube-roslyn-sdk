/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class RoslynPluginJarBuilderTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void RoslynPlugin_Test()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(TestContext);
            string workingDir = TestUtils.CreateTestDirectory(TestContext, ".working");
            string outputJarFilePath = Path.Combine(testDir, "created.jar");

            string dummyRulesFile = TestUtils.CreateTextFile("rules.txt", testDir, "<rules />");
            string dummyZipFile = TestUtils.CreateTextFile("payload.txt", testDir, "zip");

            PluginManifest manifest= new PluginManifest()
            {
                Key = "pluginkey",
                Description = "description",
                Name = "name"
            };

            // Act
            RoslynPluginJarBuilder builder = new RoslynPluginJarBuilder(new TestLogger());

            builder.SetLanguage("cs")
                .SetRepositoryKey("repo.key")
                .SetRepositoryName("repo.name")
                .SetRulesFilePath(dummyRulesFile)
                .SetPluginManifestProperties(manifest)
                .AddResourceFile(dummyZipFile, "static\\foo.zip")
                .SetJarFilePath(outputJarFilePath);

            builder.BuildJar(workingDir);

            // Assert
            ZipFileChecker checker = new ZipFileChecker(TestContext, outputJarFilePath);

            checker.AssertZipContainsFiles(
                "META-INF\\MANIFEST.MF",
                "static\\foo.zip",
                "org\\sonar\\plugins\\roslynsdk\\configuration.xml",
                "org\\sonar\\plugins\\roslynsdk\\rules.xml"
                );

            checker.AssertZipDoesNotContainFiles(
                "org\\sonar\\plugins\\roslynsdk\\sqale.xml");
        }

        #endregion Tests
    }
}