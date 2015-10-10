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

            // Act
            SourceGenerator.CreateSourceFiles(this.GetType().Assembly, "PluginGeneratorTests/resources", outputDir, replacements);

            // Assert
            AssertSourceFileExists("myorg\\myapp\\myappClass1.java", outputDir);
            AssertExpectedSourceFileCount(1, outputDir);
        }


        private static string AssertSourceFileExists(string fileName, string outputDir)
        {
            string fullPath = Path.Combine(outputDir, fileName);
            Assert.IsTrue(File.Exists(fullPath), "Expected file does not exist");

            return File.ReadAllText(fullPath);
        }

        private static void AssertExpectedSourceFileCount(int expected, string outputDir)
        {
            string[] javaFiles = Directory.GetFiles(outputDir, "*.*", SearchOption.AllDirectories);

            Assert.IsTrue(javaFiles.All(f => f.EndsWith(".java")), "Output dir should only contain java files");
            Assert.AreEqual(expected, javaFiles.Length, "Unexpected number of generated files");
        }
    }
}
