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
using System.Linq;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    /// <summary>
    /// Tests for NuGetPackageHandler.cs
    /// 
    /// There is no test for the scenario released package A depending on prerelease package B as this is not allowed.
    /// </summary>
    [TestClass]
    public class NuGetPackageHandlerTests
    {
        public TestContext TestContext { get; set; }

        private const string TestPackageId = "testPackage";
        private const string DependentPackageId = "dependentPackage";

        private const string ReleaseVersion = "1.0.0";
        private const string PreReleaseVersion = "1.0.0-RC1";

        #region Tests

        [TestMethod]
        public void NuGet_TestPackageDownload_Release_Release()
        {
            // Arrange
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");

            // Create test NuGet payload and packages
            IPackageRepository fakeRemoteRepo = CreateTestPackageWithSingleDependency(ReleaseVersion, ReleaseVersion);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(fakeRemoteRepo, targetNuGetRoot, logger);

            // Act
            // Attempt to download a package which is released with a dependency that is released
            IPackage package = handler.FetchPackage(DependentPackageId, null);

            // Assert
            AssertExpectedPackage(package, DependentPackageId, ReleaseVersion);
            // Packages should have been downloaded
            AssertPackageDownloaded(targetNuGetRoot, DependentPackageId, ReleaseVersion);
            AssertPackageDownloaded(targetNuGetRoot, TestPackageId, ReleaseVersion);
        }

        [TestMethod]
        public void NuGet_TestPackageDownload_PreRelease_Release()
        {
            // Arrange
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");

            // Create test NuGet payload and packages
            IPackageRepository fakeRemoteRepo = CreateTestPackageWithSingleDependency(PreReleaseVersion, ReleaseVersion);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(fakeRemoteRepo, targetNuGetRoot, logger);

            // Act
            // Attempt to download a package which is not released with a dependency that is released
            IPackage package = handler.FetchPackage(DependentPackageId, null);

            // Assert
            AssertExpectedPackage(package, DependentPackageId, PreReleaseVersion);
            // Packages should have been downloaded
            AssertPackageDownloaded(targetNuGetRoot, DependentPackageId, PreReleaseVersion);
            AssertPackageDownloaded(targetNuGetRoot, TestPackageId, ReleaseVersion);
        }

        [TestMethod]
        public void NuGet_TestPackageDownload_PreRelease_PreRelease()
        {
            // Arrange
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");

            // Create test NuGet payload and packages
            IPackageRepository fakeRemoteRepo = CreateTestPackageWithSingleDependency(PreReleaseVersion, PreReleaseVersion);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(fakeRemoteRepo, targetNuGetRoot, logger);

            // Act
            // Attempt to download a package which is not released with a dependency that is not released
            IPackage package = handler.FetchPackage(DependentPackageId, null);

            // Assert
            AssertExpectedPackage(package, DependentPackageId, PreReleaseVersion);
            // Packages should have been downloaded
            AssertPackageDownloaded(targetNuGetRoot, DependentPackageId, PreReleaseVersion);
            AssertPackageDownloaded(targetNuGetRoot, TestPackageId, PreReleaseVersion);
        }

        [TestMethod]
        public void FetchPackage_VersionSpecified_CorrectVersionSelected()
        {
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");

            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.8.0");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "1.0.0-rc1");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "2.0.0");

            BuildAndInstallPackage(remoteRepoBuilder, "dummy.package.1", "0.8.0");

            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.9.0");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "1.0.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, targetNuGetRoot, new TestLogger());

            // Check for specific versions
            IPackage actual = handler.FetchPackage("package.id.1", new SemanticVersion("0.8.0"));
            AssertExpectedPackage(actual, "package.id.1", "0.8.0");

            actual = handler.FetchPackage("package.id.1", new SemanticVersion("1.0.0-rc1"));
            AssertExpectedPackage(actual, "package.id.1", "1.0.0-rc1");

            actual = handler.FetchPackage("package.id.1", new SemanticVersion("2.0.0"));
            AssertExpectedPackage(actual, "package.id.1", "2.0.0");
        }

        [TestMethod]
        public void FetchPackage_VersionNotSpecified_ReleaseVersionExists_LastReleaseVersionSelected()
        {
            // Arrange
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);

            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.8.0");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.9.0-rc1");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "1.0.0");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "1.1.0-rc1");
            BuildAndInstallPackage(remoteRepoBuilder, "dummy.package.1", "2.0.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, targetNuGetRoot, new TestLogger());

            // Act
            IPackage actual = handler.FetchPackage("package.id.1", null);

            // Assert
            AssertExpectedPackage(actual, "package.id.1", "1.0.0");
        }

        [TestMethod]
        public void FetchPackage_VersionNotSpecified_NoReleaseVersions_LastPreReleaseVersionSelected()
        {
            // Arrange
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);

            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.9.0-rc1");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "1.0.0-rc1");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "1.1.0-rc1");
            BuildAndInstallPackage(remoteRepoBuilder, "dummy.package.1", "2.0.0");
            BuildAndInstallPackage(remoteRepoBuilder, "dummy.package.1", "2.0.0-rc2");

            NuGetPackageHandler handler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, targetNuGetRoot, new TestLogger());

            // Act
            IPackage actual = handler.FetchPackage("package.id.1", null);

            // Assert
            AssertExpectedPackage(actual, "package.id.1", "1.1.0-rc1");
        }

        [TestMethod]
        public void FetchPackage_PackageNotFound_NullReturned()
        {
            // Arrange
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);

            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.8.0");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.9.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, targetNuGetRoot, new TestLogger());

            // 1. Package id not found
            IPackage actual = handler.FetchPackage("unknown.package.id", new SemanticVersion("0.8.0"));
            Assert.IsNull(actual, "Not expecting a package to be found");

            // 2. Package id not found
            actual = handler.FetchPackage("package.id.1", new SemanticVersion("0.7.0"));
            Assert.IsNull(actual, "Not expecting a package to be found");
        }

        #endregion

        #region Private methods

        private IPackage BuildAndInstallPackage(RemoteRepoBuilder remoteRepoBuilder, string id, string version)
        {
            string dummyTextFile = CreateDummyContentFile();
            return remoteRepoBuilder.CreatePackage(id, version, dummyTextFile, RemoteRepoBuilder.License.NotRequired);
        }
        
        private IPackageRepository CreateTestPackageWithSingleDependency(string dependentPackageVersion, string testPackageVersion)
        {
            string dummyTextFile = CreateDummyContentFile();
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(this.TestContext);

            IPackage testPackage = remoteRepoBuilder.CreatePackage(TestPackageId, testPackageVersion, dummyTextFile, RemoteRepoBuilder.License.NotRequired);
            remoteRepoBuilder.CreatePackage(DependentPackageId, dependentPackageVersion, dummyTextFile, RemoteRepoBuilder.License.NotRequired, testPackage);

            return remoteRepoBuilder.FakeRemoteRepo;
        }

        private string CreateDummyContentFile()
        {
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext, "source");
            string dummyTextFile = TestUtils.CreateTextFile(Guid.NewGuid().ToString(), testDir, "content");
            return dummyTextFile;
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

        private void AssertPackageDownloaded(string downloadDir, string expectedName, string expectedVersion)
        {
            string packageDir = Directory.GetDirectories(downloadDir)
                .SingleOrDefault(d => d.Contains(expectedName) && d.Contains(expectedVersion));
            Assert.IsNotNull(packageDir,
                "Expected a package to have been downloaded: " + expectedName);
        }

        #endregion
    }
}