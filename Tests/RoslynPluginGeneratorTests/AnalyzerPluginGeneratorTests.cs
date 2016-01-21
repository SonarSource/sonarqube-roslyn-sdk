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
        private enum Node { Root, Child1, Child2, Grandchild1_1, Grandchild2_1, Grandchild2_2 };

        [TestMethod]
        public void Generate_PackageNoAccept_NoDependencies_Succeeds()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");            
            string localNuGetSourceDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetSource");
            string localNuGetDownloadDir = TestUtils.CreateTestDirectory(this.TestContext, ".localNuGetDownload");

            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, localNuGetSourceDir, localNuGetDownloadDir);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);

            IPackageManager localNuGetSourceStore = CreatePackageManager(localNuGetSourceDir);
            SetupTestGraph(localNuGetSourceStore);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Grandchild1_1.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsTrue(result, "Generator should succeed if there are no licenses to accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleLevelDependencies_DependenciesNoAccept_Succeeds()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()));

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Root.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsTrue(result, "Generator should succeed if there are no licenses to accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_NoDependencies_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()), Node.Grandchild1_1);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Grandchild1_1.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if the package requires license Accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_OneDependency_DependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()), Node.Grandchild1_1);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Child1.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_OneDependency_DependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()), Node.Child1, Node.Grandchild1_1);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Child1.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleDependencies_OneDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()), Node.Grandchild2_1);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Child2.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleDependencies_SecondDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()), Node.Grandchild2_2);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Child2.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_MultipleDependencies_AllDependenciesRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()), Node.Child2, Node.Grandchild2_1, Node.Grandchild2_2);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Child2.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleLevelDependencies_SecondLevelDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()), Node.Grandchild1_1);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Root.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleLevelDependencies_SecondLevelSecondDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateLocalNuGetPackageHander();

            SetupTestGraph(CreatePackageManager(GetLocalNuGetSourceDir()), Node.Grandchild2_1);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Root.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        #region Private methods

        private AnalyzerPluginGenerator CreateLocalNuGetPackageHander()
        {
            TestLogger logger = new TestLogger();
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(logger, GetLocalNuGetSourceDir(), GetLocalNuGetDownloadDir());
            return new AnalyzerPluginGenerator(nuGetHandler, logger);
        }

        /// <summary>
        /// Creates a graph used for testing, with nodes labelled in breadth-first order.
        /// 
        /// Visually:
        /// Root--------
        /// |            \
        /// Child1       Child2---------
        /// |             |             \
        /// Grandchild1_1 GrandChild2_1 GrandChild2_2
        /// </summary>
        /// <param name="nodesRequireLicense">
        /// Nodes in the graph that should be packages with the field requireLicenseAccept set to true
        /// </param>
        private void SetupTestGraph(IPackageManager mgr, params Node[] nodesRequireLicense)
        {
            // leaf nodes
            CreatePackage(mgr, IsLicenseRequiredFor(Node.Grandchild1_1, nodesRequireLicense), Node.Grandchild1_1);
            CreatePackage(mgr, IsLicenseRequiredFor(Node.Grandchild2_1, nodesRequireLicense), Node.Grandchild2_1);
            CreatePackage(mgr, IsLicenseRequiredFor(Node.Grandchild2_2, nodesRequireLicense), Node.Grandchild2_2);

            // non-leaf nodes
            CreatePackage(mgr, IsLicenseRequiredFor(Node.Child1, nodesRequireLicense), Node.Child1, Node.Grandchild1_1);
            CreatePackage(mgr, IsLicenseRequiredFor(Node.Child2, nodesRequireLicense), Node.Child2, Node.Grandchild2_1, Node.Grandchild2_2);

            // root
            CreatePackage(mgr, IsLicenseRequiredFor(Node.Root, nodesRequireLicense), Node.Root, Node.Child1, Node.Child2);
        }

        private License IsLicenseRequiredFor(Node node, Node[] nodesRequireLicense)
        {
            return nodesRequireLicense.Contains(node) ? License.Required : License.NotRequired;
        }

        private IPackageManager CreatePackageManager(string packageSource)
        {
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(packageSource);
            PackageManager mgr = new PackageManager(repo, packageSource);

            return mgr;
        }

        private void CreatePackage(IPackageManager manager, License requiresLicenseAccept, Node packageNode, params Node[] dependencyNodes)
        {
            PackageBuilder builder = new PackageBuilder();
            ManifestMetadata metadata = new ManifestMetadata()
            {
                Authors = "dummy author",
                Version = new SemanticVersion("1.0").ToString(),
                Id = packageNode.ToString(),
                Description = "dummy description",
                LicenseUrl = "http://choosealicense.com/",
                RequireLicenseAcceptance = (requiresLicenseAccept == License.Required)
            };

            List<ManifestDependency> dependencyList = new List<ManifestDependency>();
            foreach (Node dependencyNode in dependencyNodes)
            {
                dependencyList.Add(new ManifestDependency()
                {
                    Id = dependencyNode.ToString(),
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

            string fileName = packageNode.ToString() + "." + metadata.Version + ".nupkg";
            string destinationName = Path.Combine(manager.LocalRepository.Source.ToString(), fileName);
            
            using (Stream fileStream = File.Open(destinationName, FileMode.OpenOrCreate))
            {
                builder.Save(fileStream);
            }
        }

        private string GetLocalNuGetDownloadDir()
        {
            return TestUtils.EnsureTestDirectoryExists(this.TestContext, ".localNuGetDownload");
        }

        private string GetLocalNuGetSourceDir()
        {
            return TestUtils.EnsureTestDirectoryExists(this.TestContext, ".localNuGetSource");
        }

        #endregion

    }
}
