//-----------------------------------------------------------------------
// <copyright file="AnalyzerPluginGeneratorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarLint.XmlDescriptor;
using SonarQube.Plugins.Roslyn.CommandLine;
using SonarQube.Plugins.Test.Common;
using System.IO;
using System.Linq;
using static SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests.RemoteRepoBuilder;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    /// <summary>
    /// Tests for NuGetPackageHandler.cs
    /// </summary>
    [TestClass]
    public class AnalyzerPluginGeneratorTests
    {
        public TestContext TestContext { get; set; }

        private enum Node { Root, Child1, Child2, Grandchild1_1, Grandchild2_1, Grandchild2_2 };

        [TestMethod]
        public void Generate_NoAnalyzersFoundInPackage_GenerateFails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            TestLogger logger = new TestLogger();

            // Create a fake remote repo containing a package that does not contain analyzers
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            remoteRepoBuilder.CreatePackage("no.analyzers.id", "0.9", TestUtils.CreateTextFile("dummy.txt", outputDir), License.NotRequired /* no dependencies */ );
           
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            ProcessedArgs args = CreateArgs("no.analyzers.id", "0.9", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Expecting generation to fail");
            logger.AssertWarningsLogged(1);
            AssertSqaleTemplateDoesNotExist(outputDir);

        }

        [TestMethod]
        public void Generate_PackageNoAccept_NoDependencies_Succeeds()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder);

            ProcessedArgs args = CreateArgs(Node.Grandchild1_1.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsTrue(result, "Generator should succeed if there are no licenses to accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultiLevel_DependenciesNoAccept_Succeeds()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder);

            ProcessedArgs args = CreateArgs(Node.Root.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsTrue(result, "Generator should succeed if there are no licenses to accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_NoDependencies_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder, Node.Grandchild1_1);

            ProcessedArgs args = CreateArgs(Node.Grandchild1_1.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Generator should fail if the package requires license Accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_OneDependency_DependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder, Node.Grandchild1_1);

            ProcessedArgs args = CreateArgs(Node.Child1.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_OneDependency_DependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder, Node.Child1, Node.Grandchild1_1);

            ProcessedArgs args = CreateArgs(Node.Child1.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleDependencies_OneDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder, Node.Grandchild2_1);

            ProcessedArgs args = CreateArgs(Node.Child2.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultipleDependencies_SecondDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder, Node.Grandchild2_2);

            ProcessedArgs args = CreateArgs(Node.Child2.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageRequiresAccept_MultipleDependencies_AllDependenciesRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder, Node.Child2, Node.Grandchild2_1, Node.Grandchild2_2);

            ProcessedArgs args = CreateArgs(Node.Child2.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultiLevel_SecondLevelDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder, Node.Grandchild1_1);

            ProcessedArgs args = CreateArgs(Node.Root.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_PackageNoAccept_MultiLevel_SecondLevelSecondDependencyRequiresAccept_Fails()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            SetupTestGraph(remoteRepoBuilder, Node.Grandchild2_1);

            ProcessedArgs args = CreateArgs(Node.Root.ToString(), "1.0", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Generator should fail if a package dependency requires license accept");
        }

        [TestMethod]
        public void Generate_SqaleFileNotSpecified_TemplateFileCreated()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            TestLogger logger = new TestLogger();

            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            CreatePackageInFakeRemoteRepo(remoteRepoBuilder, "dummy.id", "1.1");

            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);

            string expectedTemplateSqaleFilePath = Path.Combine(outputDir, "dummy.id.1.1.sqale.template.xml");

            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);

            ProcessedArgs args = CreateArgs("dummy.id", "1.1", "cs", null, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsTrue(result, "Expecting generation to have succeeded");
            Assert.IsTrue(File.Exists(expectedTemplateSqaleFilePath), "Expecting a template sqale file to have been created");
            this.TestContext.AddResultFile(expectedTemplateSqaleFilePath);
            logger.AssertSingleInfoMessageExists(expectedTemplateSqaleFilePath); // should be a message about the generated file
        }

        [TestMethod]
        public void Generate_ValidSqaleFileSpecified_TemplateFileNotCreated()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            AnalyzerPluginGenerator apg = CreateTestSubjectWithFakeRemoteRepo(remoteRepoBuilder);

            CreatePackageInFakeRemoteRepo(remoteRepoBuilder, "dummy.id", "1.1");

            // Create a dummy sqale file
            string dummySqaleFilePath = Path.Combine(outputDir, "inputSqale.xml");
            SqaleRoot dummySqale = new SqaleRoot();
            Serializer.SaveModel(dummySqale, dummySqaleFilePath);

            ProcessedArgs args = CreateArgs("dummy.id", "1.1", "cs", dummySqaleFilePath, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsTrue(result, "Expecting generation to have succeeded");
            AssertSqaleTemplateDoesNotExist(outputDir);
        }

        [TestMethod]
        public void Generate_InvalidSqaleFileSpecified_GeneratorError()
        {
            // Arrange
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            TestLogger logger = new TestLogger();

            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            CreatePackageInFakeRemoteRepo(remoteRepoBuilder, "dummy.id", "1.1");

            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);

            // Create an invalid sqale file
            string dummySqaleFilePath = Path.Combine(outputDir, "invalidSqale.xml");
            File.WriteAllText(dummySqaleFilePath, "not valid xml");

            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);

            ProcessedArgs args = CreateArgs("dummy.id", "1.1", "cs", dummySqaleFilePath, false, outputDir);

            // Act
            bool result = apg.Generate(args);

            // Assert
            Assert.IsFalse(result, "Expecting generation to have failed");
            AssertSqaleTemplateDoesNotExist(outputDir);
            logger.AssertSingleErrorExists("invalidSqale.xml"); // expecting an error containing the invalid sqale file name
        }

        #region Private methods

        private static ProcessedArgs CreateArgs(string packageId, string packageVersion, string language, string sqaleFilePath, bool acceptLicenses, string outputDirectory)
        {
            ProcessedArgs args = new ProcessedArgs(
                packageId,
                new SemanticVersion(packageVersion),
                language,
                sqaleFilePath,
                acceptLicenses,
                outputDirectory);
            return args;
        }

        private AnalyzerPluginGenerator CreateTestSubjectWithFakeRemoteRepo(RemoteRepoBuilder remoteRepoBuilder)
        {
            TestLogger logger = new TestLogger();
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, GetLocalNuGetDownloadDir(), logger);
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
        private void SetupTestGraph(RemoteRepoBuilder remoteRepoBuilder, params Node[] nodesRequireLicense)
        {
            // leaf nodes
            IPackage grandChild1_1 = CreatePackage(remoteRepoBuilder, Node.Grandchild1_1, nodesRequireLicense);
            IPackage grandChild2_1 = CreatePackage(remoteRepoBuilder, Node.Grandchild2_1, nodesRequireLicense);
            IPackage grandChild2_2 = CreatePackage(remoteRepoBuilder, Node.Grandchild2_2, nodesRequireLicense);

            // non-leaf nodes
            IPackage child1 = CreatePackage(remoteRepoBuilder, Node.Child1, nodesRequireLicense, grandChild1_1);
            IPackage child2 = CreatePackage(remoteRepoBuilder, Node.Child2, nodesRequireLicense, grandChild2_1, grandChild2_2);

            // root
            CreatePackage(remoteRepoBuilder, Node.Root, nodesRequireLicense, child1, child2);
        }

        private void CreatePackageInFakeRemoteRepo(RemoteRepoBuilder remoteRepoBuilder, string packageId, string packageVersion)
        {
            remoteRepoBuilder.CreatePackage(packageId, packageVersion, typeof(RoslynAnalyzer11.AbstractAnalyzer).Assembly.Location, License.NotRequired /* no dependencies */ );
        }

        private IPackage CreatePackage(RemoteRepoBuilder remoteRepoBuilder, Node packageNode, Node[] nodesRequireLicense, params IPackage[] dependencyNodes)
        {
            return remoteRepoBuilder.CreatePackage(packageNode.ToString(), "1.0",
                typeof(RoslynAnalyzer11.CSharpAnalyzer).Assembly.Location, // generation will fail unless there are analyzers to process
                IsLicenseRequiredFor(packageNode, nodesRequireLicense),
                dependencyNodes);
        }

        private License IsLicenseRequiredFor(Node node, Node[] nodesRequireLicense)
        {
            return nodesRequireLicense.Contains(node) ? License.Required : License.NotRequired;
        }
        
        private string GetLocalNuGetDownloadDir()
        {
            return TestUtils.EnsureTestDirectoryExists(this.TestContext, ".localNuGetDownload");
        }

        private static void AssertSqaleTemplateDoesNotExist(string outputDir)
        {
            string[] matches = Directory.GetFiles(outputDir, "*sqale*template*", SearchOption.AllDirectories);
            Assert.AreEqual(0, matches.Length, "Not expecting any squale template files to exist");
        }

        #endregion

    }
}
