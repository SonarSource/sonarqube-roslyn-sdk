//-----------------------------------------------------------------------
// <copyright file="RoslynPluginJarBuilderTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;

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
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string workingDir = TestUtils.CreateTestDirectory(this.TestContext, ".working");
            string outputJarFilePath = Path.Combine(testDir, "created.jar");

            string dummyRulesFile = TestUtils.CreateTextFile("rules.txt", testDir, "<rules />");
            string dummySqaleFile = TestUtils.CreateTextFile("sqale.txt", testDir, "<sqale />");
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
                .SetSqaleFilePath(dummySqaleFile)
                .SetPluginManifestProperties(manifest)
                .AddResourceFile(dummyZipFile, "static\\foo.zip")
                .SetJarFilePath(outputJarFilePath);
            
            builder.BuildJar(workingDir);

            // Assert
            ZipFileChecker checker = new ZipFileChecker(this.TestContext, outputJarFilePath);

            checker.AssertZipContainsFiles(
                "META-INF\\MANIFEST.MF",
                "static\\foo.zip",
                "org\\sonar\\plugins\\roslynsdk\\configuration.xml",
                "org\\sonar\\plugins\\roslynsdk\\sqale.xml",
                "org\\sonar\\plugins\\roslynsdk\\rules.xml"
                );
        }

        #endregion

    }
}
