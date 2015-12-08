//-----------------------------------------------------------------------
// <copyright file="SourceGeneratorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class SourceGeneratorTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void GenerateSource()
        {
            // Arrange
            string outputDir = Path.Combine(this.TestContext.TestDeploymentDir, this.TestContext.TestName);
            IDictionary<string, string> replacements = new Dictionary<string, string>();
            replacements.Add("[REPLACE1]", "111");
            replacements.Add("[REPLACE2]", "222");

            // Act
            SourceGenerator.CreateSourceFiles(this.GetType().Assembly, "SonarQube.Plugins.PluginGeneratorTests.resources", outputDir, replacements);

            // Assert
            string content;
            content = TestUtils.AssertFileExists("myorg\\myappClass1.java", outputDir);
            Assert.AreEqual("XXX\r\n111222\r\nYYY", content, "Unexpected file content");

            content = TestUtils.AssertFileExists("myorg\\myapp\\myappClass2.java", outputDir);
            Assert.AreEqual("111zzz", content, "Unexpected file content");

            AssertExpectedSourceFileCount(2, outputDir);
        }

        [TestMethod] // Second test to check the handling of nesting
        public void GenerateSource2()
        {
            // Arrange
            string outputDir = Path.Combine(this.TestContext.TestDeploymentDir, this.TestContext.TestName);
            IDictionary<string, string> replacements = new Dictionary<string, string>();
            replacements.Add("[REPLACE1]", "111");
            replacements.Add("[REPLACE2]", "222");

            // Act
            SourceGenerator.CreateSourceFiles(this.GetType().Assembly, "SonarQube.Plugins.PluginGeneratorTests.resources.myorg.myapp", outputDir, replacements);

            // Assert
            string content;
            content = TestUtils.AssertFileExists("myappClass2.java", outputDir);
            Assert.AreEqual("111zzz", content, "Unexpected file content");

            AssertExpectedSourceFileCount(1, outputDir);
        }

        private static void AssertExpectedSourceFileCount(int expected, string outputDir)
        {
            string[] javaFiles = Directory.GetFiles(outputDir, "*.*", SearchOption.AllDirectories);

            Assert.IsTrue(javaFiles.All(f => f.EndsWith(".java")), "Output dir should only contain java files");
            Assert.AreEqual(expected, javaFiles.Length, "Unexpected number of generated files in the output folder: {0}", outputDir);
        }
    }
}
