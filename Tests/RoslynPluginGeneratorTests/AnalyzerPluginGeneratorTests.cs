//-----------------------------------------------------------------------
// <copyright file="AnalyzerPluginGeneratorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;
using NuGet;
using System.Linq;
using System.Collections.Generic;
namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    /// <summary>
    /// Tests for NuGetPackageHandler.cs
    /// </summary>
    [TestClass]
    public class AnalyzerPluginGeneratorTests
    {
        public TestContext TestContext { get; set; }

        private enum License { Required, NotRequired };
        private enum Node { A, B, C, D, E, F };

        [TestMethod]
        public void Generate_PackageNoAccept_NoDependencies_Succeeds()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            
            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.D.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsTrue(result, "Generator should succeed if there are no licenses to accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleLevelDependencies_DependenciesNoAccept_Succeeds()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.A.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsTrue(result, "Generator should succeed if there are no licenses to accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_NoDependencies_Fails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore, Node.D);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.D.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsFalse(result, "Generator should fail if the package requires license Accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_OneDependency_DependencyRequiresAccept_Fails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore, Node.D);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.B.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_OneDependency_DependencyRequiresAccept_Fails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore, Node.B, Node.D);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.B.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleDependencies_OneDependencyRequiresAccept_Fails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore, Node.E);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.C.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleDependencies_SecondDependencyRequiresAccept_Fails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore, Node.F);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.C.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_MultipleDependencies_AllDependenciesRequiresAccept_Fails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore, Node.C, Node.E, Node.F);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.C.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleLevelDependencies_SecondLevelDependencyRequiresAccept_Fails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore, Node.D);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.A.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleLevelDependencies_SecondLevelSecondDependencyRequiresAccept_Fails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");
            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore, Node.E);

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.A.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            UninstallAllTestNuGetPackages(localNuGetSourceStore);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        #region Private methods

        private void UninstallAllTestNuGetPackages(IPackageManager manager)
        {
            foreach (Node node in Enum.GetValues(typeof(Node)))
            {
                manager.UninstallPackage(node.ToString(), new SemanticVersion("1.0"), true, false);
            }
        }

        /// <summary>
        /// Creates a graph used for testing, with nodes labelled in breadth-first order.
        /// 
        /// Visually:
        /// A
        /// |\
        /// B C 
        /// | |\
        /// D E F
        /// </summary>
        /// <param name="nodesRequireLicense">
        /// Nodes in the graph that should be packages with the field requireLicenseAccept set to true
        /// </param>
        private void SetupTestGraph(IPackageManager mgr, params Node[] nodesRequireLicense)
        {
            // leaf nodes
            CreatePackage(mgr, nodesRequireLicense.Contains(Node.D) ? License.Required : License.NotRequired, Node.D.ToString());
            CreatePackage(mgr, nodesRequireLicense.Contains(Node.E) ? License.Required : License.NotRequired, Node.E.ToString());
            CreatePackage(mgr, nodesRequireLicense.Contains(Node.F) ? License.Required : License.NotRequired, Node.F.ToString());

            // non-leaf nodes
            CreatePackage(mgr, nodesRequireLicense.Contains(Node.B) ? License.Required : License.NotRequired, Node.B.ToString(),
                Node.D.ToString());
            CreatePackage(mgr, nodesRequireLicense.Contains(Node.C) ? License.Required : License.NotRequired, Node.C.ToString(),
                Node.E.ToString(), Node.F.ToString());

            // root
            CreatePackage(mgr, nodesRequireLicense.Contains(Node.A) ? License.Required : License.NotRequired, Node.A.ToString(),
                Node.B.ToString(), Node.C.ToString());
        }

        private IPackageManager CreatePackageManager(string packageSource)
        {
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(packageSource);
            PackageManager mgr = new PackageManager(repo, packageSource);

            return mgr;
        }

        private void CreatePackage(IPackageManager manager, License requiresLicenseAccept, string id, params string[] dependencyIds)
        {
            PackageBuilder builder = new PackageBuilder();
            ManifestMetadata metadata = new ManifestMetadata()
            {
                Authors = "dummy author",
                Version = new SemanticVersion("1.0").ToString(),
                Id = id,
                Description = "dummy description",
                LicenseUrl = "http://choosealicense.com/",
                RequireLicenseAcceptance = (requiresLicenseAccept == License.Required)
            };

            List<ManifestDependency> dependencyList = new List<ManifestDependency>();
            foreach (string dependencyId in dependencyIds)
            {
                dependencyList.Add(new ManifestDependency()
                {
                    Id = dependencyId,
                    Version = new SemanticVersion("1.0").ToString(),
                });
            }

            List<ManifestDependencySet> dependencySetList = new List<ManifestDependencySet>()
            {
                new ManifestDependencySet()
                {
                    Dependencies = dependencyList
                }
            };
            metadata.DependencySets = dependencySetList;

            builder.Populate(metadata);

            // dummy payload
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext, "source");
            string dummyTextFile = TestUtils.CreateTextFile("blank.txt", testDir, "content");

            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = dummyTextFile;
            file.TargetPath = "dummy.txt";
            builder.Files.Add(file);

            string fileName = id + "." + metadata.Version + ".nupkg";
            string destinationName = Path.Combine(manager.LocalRepository.Source.ToString(), fileName);
            
            using (Stream fileStream = File.Open(destinationName, FileMode.OpenOrCreate))
            {
                builder.Save(fileStream);
            }
        }

        #endregion

    }
}
