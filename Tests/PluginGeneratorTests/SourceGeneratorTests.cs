using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PluginGenerator;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace PluginGeneratorTests
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
            SourceGenerator.CreateSourceFiles(this.GetType().Assembly, "PluginGeneratorTests.resources", outputDir, replacements);

            // Assert
            string content;
            content = AssertSourceFileExists("myorg\\myappClass1.java", outputDir);
            Assert.AreEqual("XXX\r\n111222\r\nYYY", content, "Unexpected file content");

            content = AssertSourceFileExists("myorg\\myapp\\myappClass2.java", outputDir);
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
            SourceGenerator.CreateSourceFiles(this.GetType().Assembly, "PluginGeneratorTests.resources.myorg.myapp", outputDir, replacements);

            // Assert
            string content;
            content = AssertSourceFileExists("myappClass2.java", outputDir);
            Assert.AreEqual("111zzz", content, "Unexpected file content");

            AssertExpectedSourceFileCount(1, outputDir);
        }


        private static string AssertSourceFileExists(string fileName, string outputDir)
        {
            string fullPath = Path.Combine(outputDir, fileName);
            Assert.IsTrue(File.Exists(fullPath), "Expected file does not exist: {0}", fullPath);

            return File.ReadAllText(fullPath);
        }

        private static void AssertExpectedSourceFileCount(int expected, string outputDir)
        {
            string[] javaFiles = Directory.GetFiles(outputDir, "*.*", SearchOption.AllDirectories);

            Assert.IsTrue(javaFiles.All(f => f.EndsWith(".java")), "Output dir should only contain java files");
            Assert.AreEqual(expected, javaFiles.Length, "Unexpected number of generated files in the output folder: {0}", outputDir);
        }
    }
}
