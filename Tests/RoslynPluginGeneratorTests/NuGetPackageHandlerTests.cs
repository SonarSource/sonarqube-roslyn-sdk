//-----------------------------------------------------------------------
// <copyright file="NuGetPackageHandlerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;
using NuGet;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class NuGetPackageHandlerTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void NuGet_TestDependencyResolutionFailure()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(testDir, logger);

            // Act
            // Fetch a package that should fail due to pre-release dependencies
            IPackage package = handler.FetchPackage("codeCracker", null, testDir);

            // Assert
            // No files should have been downloaded
            Assert.IsTrue(Directory.GetFiles(testDir, "*.*", SearchOption.AllDirectories).Length == 0);
            Assert.IsNull(package);
        }

        [TestMethod]
        public void FetchPackage_VersionSpecified_CorrectVersionSelected()
        {
            string sourceNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.source");
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            IPackageManager mgr = CreatePackageManager(sourceNuGetRoot);

            AddPackage(mgr, "package.id.1", "0.8.0");
            AddPackage(mgr, "package.id.1", "1.0.0-rc1");
            AddPackage(mgr, "package.id.1", "2.0.0");

            AddPackage(mgr, "dummy.package.1", "0.8.0");

            AddPackage(mgr, "package.id.1", "0.9.0");
            AddPackage(mgr, "package.id.1", "1.0.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(sourceNuGetRoot, new TestLogger());

            // Check for specific versions
            IPackage actual = handler.FetchPackage("package.id.1", new SemanticVersion("0.8.0"), targetNuGetRoot);
            AssertExpectedPackage(actual, "package.id.1", "0.8.0");

            actual = handler.FetchPackage("package.id.1", new SemanticVersion("1.0.0-rc1"), targetNuGetRoot);
            AssertExpectedPackage(actual, "package.id.1", "1.0.0-rc1");

            actual = handler.FetchPackage("package.id.1", new SemanticVersion("2.0.0"), targetNuGetRoot);
            AssertExpectedPackage(actual, "package.id.1", "2.0.0");
        }

        [TestMethod]
        public void FetchPackage_VersionNotSpecified_ReleaseVersionExists_LastReleaseVersionSelected()
        {
            // Arrange
            string sourceNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.source");
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            IPackageManager mgr = CreatePackageManager(sourceNuGetRoot);

            AddPackage(mgr, "package.id.1", "0.8.0");
            AddPackage(mgr, "package.id.1", "0.9.0-rc1");
            AddPackage(mgr, "package.id.1", "1.0.0");
            AddPackage(mgr, "package.id.1", "1.1.0-rc1");
            AddPackage(mgr, "dummy.package.1", "2.0.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(sourceNuGetRoot, new TestLogger());

            // Act
            IPackage actual = handler.FetchPackage("package.id.1", null, targetNuGetRoot);

            // Assert
            AssertExpectedPackage(actual, "package.id.1", "1.0.0");
        }

        [TestMethod]
        public void FetchPackage_VersionNotSpecified_NoReleaseVersions_LastPreReleaseVersionSelected()
        {
            // Arrange
            string sourceNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.source");
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            IPackageManager mgr = CreatePackageManager(sourceNuGetRoot);

            AddPackage(mgr, "package.id.1", "0.9.0-rc1");
            AddPackage(mgr, "package.id.1", "1.0.0-rc1");
            AddPackage(mgr, "package.id.1", "1.1.0-rc1");
            AddPackage(mgr, "dummy.package.1", "2.0.0");
            AddPackage(mgr, "dummy.package.1", "2.0.0-rc2");

            NuGetPackageHandler handler = new NuGetPackageHandler(sourceNuGetRoot, new TestLogger());

            // Act
            IPackage actual = handler.FetchPackage("package.id.1", null, targetNuGetRoot);

            // Assert
            AssertExpectedPackage(actual, "package.id.1", "1.1.0-rc1");
        }

        [TestMethod]
        public void FetchPackage_PackageNotFound_NullReturned()
        {
            // Arrange
            string sourceNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.source");
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            IPackageManager mgr = CreatePackageManager(sourceNuGetRoot);

            AddPackage(mgr, "package.id.1", "0.8.0");
            AddPackage(mgr, "package.id.1", "0.9.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(sourceNuGetRoot, new TestLogger());

            // 1. Package id not found
            IPackage actual = handler.FetchPackage("unknown.package.id", new SemanticVersion("0.8.0"), targetNuGetRoot);
            Assert.IsNull(actual, "Not expecting a package to be found");

            // 2. Package id not found
            actual = handler.FetchPackage("package.id.1", new SemanticVersion("0.7.0"), targetNuGetRoot);
            Assert.IsNull(actual, "Not expecting a package to be found");
        }

        #endregion

        #region Private methods

        private IPackageManager CreatePackageManager(string rootDir)
        {
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(rootDir);
            PackageManager mgr = new PackageManager(repo, rootDir);

            return mgr;
        }

        private void AddPackage(IPackageManager manager, string id, string version)
        {
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext, "source");
            string dummyTextFile = TestUtils.CreateTextFile(Guid.NewGuid().ToString(), testDir, "content");

            PackageBuilder builder = new PackageBuilder();
            builder.Id = id;
            builder.Version = new SemanticVersion(version);
            builder.Description = "dummy description";
            builder.Authors.Add("dummy author");

            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = dummyTextFile;
            file.TargetPath = "dummy.txt";
            builder.Files.Add(file);

            MemoryStream stream = new MemoryStream();
            builder.Save(stream);
            stream.Position = 0;

            ZipPackage pkg = new ZipPackage(stream);            
            manager.InstallPackage(pkg, true, true);
        }

        #endregion

        #region Checks

        private static void AssertExpectedPackage(IPackage actual, string expectedId, string expectedVersion)
        {
            Assert.IsNotNull(actual, "The package should not be null");

            SemanticVersion sVersion = new SemanticVersion(expectedVersion);

            Assert.AreEqual(expectedId, actual.Id, "Unexpected package id");
            Assert.AreEqual(sVersion, actual.Version, "Unexpected package version");
        }

        #endregion
    }
}