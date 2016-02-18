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
using SonarQube.Plugins.Roslyn.CommandLine;
using SonarLint.XmlDescriptor;

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
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph();

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Grandchild1_1.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsTrue(result, "Generator should succeed if there are no licenses to accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultiLevel_DependenciesNoAccept_Succeeds()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph();

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
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph(Node.Grandchild1_1);

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
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph(Node.Grandchild1_1);

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
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph(Node.Child1, Node.Grandchild1_1);

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
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph(Node.Grandchild2_1);

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
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph(Node.Grandchild2_2);

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
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph(Node.Child2, Node.Grandchild2_1, Node.Grandchild2_2);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Child2.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultiLevel_SecondLevelDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph(Node.Grandchild1_1);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Root.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultiLevel_SecondLevelSecondDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();

            SetupTestGraph(Node.Grandchild2_1);

            // Act
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(Node.Root.ToString(), new SemanticVersion("1.0")), "cs", null, outputDir);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_SqaleFileNotSpecified_TemplateFileCreated()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();
            CreatePackageInFakeRemoteRepo("dummy.id", "1.1");

            string expectedSqaleFilePath = Path.Combine(outputDir, AnalyzerPluginGenerator.SqaleTemplateFileName);

            // Act
            bool result = apg.Generate(new NuGetReference("dummy.id", new SemanticVersion("1.1")), "cs", null, outputDir);

            // Assert
            Assert.IsTrue(result, "Expecting generation to have succeeded");
            Assert.IsTrue(DoesTemplateSqaleFileExists(outputDir), "Expecting a template sqale file to have been created");
            this.TestContext.AddResultFile(expectedSqaleFilePath);
        }

        [TestMethod]
        public void Generate_ValidSqaleFileSpecified_TemplateFileNotCreated()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo();
            CreatePackageInFakeRemoteRepo("dummy.id", "1.1");

            // Create a dummy sqale file
            string dummySqaleFilePath = Path.Combine(outputDir, "inputSqale.xml");
            SqaleRoot dummySqale = new SqaleRoot();
            Serializer.SaveModel(dummySqale, dummySqaleFilePath);

            // Act
            bool result = apg.Generate(new NuGetReference("dummy.id", new SemanticVersion("1.1")), "cs", dummySqaleFilePath, outputDir);

            // Assert
            Assert.IsTrue(result, "Expecting generation to have succeeded");
            Assert.IsFalse(DoesTemplateSqaleFileExists(outputDir), "Not expecting a template sqale file to have been created because a sqale file was supplied");
        }

        [TestMethod]
        public void Generate_InvalidSqaleFileSpecified_GeneratorError()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            TestLogger logger = new TestLogger();
            IPackageRepository fakeRemoteRepo = new LocalPackageRepository(GetFakeRemoteNuGetSourceDir());
            CreatePackageInFakeRemoteRepo("dummy.id", "1.1");

            // Create an invalid sqale file
            string dummySqaleFilePath = Path.Combine(outputDir, "invalidSqale.xml");
            File.WriteAllText(dummySqaleFilePath, "not valid xml");

            string templateSqaleFilePath = Path.Combine(outputDir, AnalyzerPluginGenerator.SqaleTemplateFileName);

            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);

            // Act
            bool result = apg.Generate(new NuGetReference("dummy.id", new SemanticVersion("1.1")), "cs", dummySqaleFilePath, outputDir);

            // Assert
            Assert.IsFalse(result, "Expecting generation to have failed");
            Assert.IsFalse(DoesTemplateSqaleFileExists(outputDir), "Not expecting a template sqale file to have been created because a sqale file was supplied");
            logger.AssertSingleErrorExists("invalidSqale.xml"); // expecting an error containing the invalid sqale file name
        }

        #region Private methods

        private AnalyzerPluginGenerator CreateTestSubjectWithFakeRemoteRepo()
        {
            TestLogger logger = new TestLogger();
            IPackageRepository fakeRemoteRepo = new LocalPackageRepository(GetFakeRemoteNuGetSourceDir());

            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);
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
        private void SetupTestGraph(params Node[] nodesRequireLicense)
        {
            string packageSource = GetFakeRemoteNuGetSourceDir();
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(packageSource);
            PackageManager mgr = new PackageManager(repo, packageSource);

            // leaf nodes
            CreatePackage(mgr, Node.Grandchild1_1, nodesRequireLicense);
            CreatePackage(mgr, Node.Grandchild2_1, nodesRequireLicense);
            CreatePackage(mgr, Node.Grandchild2_2, nodesRequireLicense);

            // non-leaf nodes
            CreatePackage(mgr, Node.Child1, nodesRequireLicense, Node.Grandchild1_1);
            CreatePackage(mgr, Node.Child2, nodesRequireLicense, Node.Grandchild2_1, Node.Grandchild2_2);

            // root
            CreatePackage(mgr, Node.Root, nodesRequireLicense, Node.Child1, Node.Child2);
        }

        private void CreatePackageInFakeRemoteRepo(string packageId, string packageVersion)
        {
            string packageSource = GetFakeRemoteNuGetSourceDir();
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(packageSource);
            PackageManager mgr = new PackageManager(repo, packageSource);

            CreatePackage(mgr, packageId, packageVersion, typeof(RoslynAnalyzer11.AbstractAnalyzer).Assembly.Location, License.NotRequired /* no dependencies */ );
        }

        private void CreatePackage(IPackageManager manager, Node packageNode, Node[] nodesRequireLicense, params Node[] dependencyNodes)
        {
            CreatePackage(manager, packageNode.ToString(), "1.0",
                typeof(RoslynAnalyzer11.CSharpAnalyzer).Assembly.Location, // generation will fail unless there are analyzers to process
                IsLicenseRequiredFor(packageNode, nodesRequireLicense),
                dependencyNodes.Select(n => n.ToString()).ToArray());
        }

        private License IsLicenseRequiredFor(Node node, Node[] nodesRequireLicense)
        {
            return nodesRequireLicense.Contains(node) ? License.Required : License.NotRequired;
        }
        
        private void CreatePackage(IPackageManager manager,
            string packageId,
            string packageVersion,
            string contentFilePath,
            License requiresLicenseAccept,
            params string[] dependencyIds)
        {
            PackageBuilder builder = new PackageBuilder();
            ManifestMetadata metadata = new ManifestMetadata()
            {
                Authors = "dummy author",
                Version = new SemanticVersion(packageVersion).ToString(),
                Id = packageId,
                Description = "dummy description",
                LicenseUrl = "http://choosealicense.com/",
                RequireLicenseAcceptance = (requiresLicenseAccept == License.Required)
            };

            List<ManifestDependency> dependencyList = new List<ManifestDependency>();
            foreach (string dependencyNode in dependencyIds)
            {
                dependencyList.Add(new ManifestDependency()
                {
                    Id = dependencyNode,
                    Version = new SemanticVersion(packageVersion).ToString(),
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

            string fileToEmbed = contentFilePath;
            // Create a dummy payload if required
            if (fileToEmbed == null)
            {
                string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext, "source");
                fileToEmbed = TestUtils.CreateTextFile("blank.txt", testDir, "content");
            }

            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = fileToEmbed;
            file.TargetPath = Path.GetFileName(fileToEmbed);
            builder.Files.Add(file);

            string fileName = packageId.ToString() + "." + metadata.Version + ".nupkg";
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

        private string GetFakeRemoteNuGetSourceDir()
        {
            return TestUtils.EnsureTestDirectoryExists(this.TestContext, ".fakeRemoteNuGetSource");
        }

        private static bool DoesTemplateSqaleFileExists(string directory)
        {
            string filePath = Path.Combine(directory, AnalyzerPluginGenerator.SqaleTemplateFileName);
            return File.Exists(filePath);
        }
        #endregion

    }
}
