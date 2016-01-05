//-----------------------------------------------------------------------
// <copyright file="RoslynGenTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Roslyn;
using SonarQube.Plugins.Test.Common;
using System.IO;
using System.Linq;

namespace SonarQube.Plugins.IntegrationTests
{
    [TestClass]
    public class RoslynGenTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [Ignore] // WIP
        public void RoslynGen()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            PluginInspector inspector = CreatePluginInspector(logger);

            // Create a valid analyzer package
            string localNuGetDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGet");
            IPackageManager localNuGetStore = CreatePackageManager(localNuGetDir);
            AddPackage(localNuGetStore, "analyzer1", "1.0", typeof(ExampleAnalyzer1.CSharpAnalyzer).Assembly.Location);



            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(localNuGetDir, logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference("analyzer1", new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsTrue(result);
            string jarFilePath = AssertPluginJarExists(outputDir);

            object description = inspector.GetPluginDescription(jarFilePath);

        }

        #region Private methods

        private PluginInspector CreatePluginInspector(Common.ILogger logger)
        {
            //// Build the Java inspector class
            string tempDir = TestUtils.CreateTestDirectory(this.TestContext, "pluginInsp");
            PluginInspector inspector = new PluginInspector(logger, tempDir);

            return inspector;
        }

        private IPackageManager CreatePackageManager(string rootDir)
        {
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(rootDir);
            PackageManager mgr = new PackageManager(repo, rootDir);

            return mgr;
        }

        private void AddPackage(IPackageManager manager, string id, string version, string payloadAssemblyFilePath)
        {
            PackageBuilder builder = new PackageBuilder();
            builder.Id = id;
            builder.Version = new SemanticVersion(version);
            builder.Description = "dummy description";
            builder.Authors.Add("dummy author");

            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = payloadAssemblyFilePath;
            file.TargetPath = "analyzers/" + Path.GetFileName(payloadAssemblyFilePath);
            builder.Files.Add(file);

            using (MemoryStream stream = new MemoryStream())
            {
                builder.Save(stream);
                stream.Position = 0;

                ZipPackage pkg = new ZipPackage(stream);
                manager.InstallPackage(pkg, true, true);
            }
        }

        #endregion

        #region Checks

        private static string AssertPluginJarExists(string rootDir)
        {
            string[] files = Directory.GetFiles(rootDir, "*.jar", SearchOption.TopDirectoryOnly);
            Assert.AreEqual(1, files.Length, "Expecting one and only one jar file to be created");
            return files.First();
        }

        #endregion
    }
}
