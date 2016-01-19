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
using System.Collections.Generic;

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

        private const string TestPackageName = "testPackage";
        private const string DependentPackageName = "dependentPackage";

        private const string ReleaseVersion = "1.0.0";
        private const string PreReleaseVersion = "1.0.0-RC1";

        #region Tests

        [TestMethod]
        public void NuGet_TestPackageDownload_Release_Release()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string testDownloadDir = Path.Combine(testDir, "download");

            // Create test NuGet payload and packages
            BuildTestPackages(true, true);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(logger, testDir, testDownloadDir);

            // Act
            // Attempt to download a package which is released with a dependency that is released
            IPackage package = handler.FetchPackage(DependentPackageName, null);

            // Assert
            AssertExpectedPackage(package, DependentPackageName, ReleaseVersion);
            // Packages should have been downloaded
            AssertPackageDownloaded(testDownloadDir, DependentPackageName, ReleaseVersion);
            AssertPackageDownloaded(testDownloadDir, TestPackageName, ReleaseVersion);
        }

        [TestMethod]
        public void NuGet_TestPackageDownload_PreRelease_Release()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string testDownloadDir = Path.Combine(testDir, "download");

            // Create test NuGet payload and packages
            BuildTestPackages(false, true);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(logger, testDir, testDownloadDir);

            // Act
            // Attempt to download a package which is not released with a dependency that is released
            IPackage package = handler.FetchPackage(DependentPackageName, null);

            // Assert
            AssertExpectedPackage(package, DependentPackageName, PreReleaseVersion);
            // Packages should have been downloaded
            AssertPackageDownloaded(testDownloadDir, DependentPackageName, PreReleaseVersion);
            AssertPackageDownloaded(testDownloadDir, TestPackageName, ReleaseVersion);
        }

        [TestMethod]
        public void NuGet_TestPackageDownload_PreRelease_PreRelease()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string testDownloadDir = Path.Combine(testDir, "download");

            // Create test NuGet payload and packages
            BuildTestPackages(false, false);

            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(logger, testDir, testDownloadDir);

            // Act
            // Attempt to download a package which is not released with a dependency that is not released
            IPackage package = handler.FetchPackage(DependentPackageName, null);

            // Assert
            AssertExpectedPackage(package, DependentPackageName, PreReleaseVersion);
            // Packages should have been downloaded
            AssertPackageDownloaded(testDownloadDir, DependentPackageName, PreReleaseVersion);
            AssertPackageDownloaded(testDownloadDir, TestPackageName, PreReleaseVersion);
        }

        [TestMethod]
        public void FetchPackage_VersionSpecified_CorrectVersionSelected()
        {
            string sourceNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.source");
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            IPackageManager mgr = CreatePackageManager(sourceNuGetRoot);

            BuildAndInstallPackage(mgr, "package.id.1", "0.8.0");
            BuildAndInstallPackage(mgr, "package.id.1", "1.0.0-rc1");
            BuildAndInstallPackage(mgr, "package.id.1", "2.0.0");

            BuildAndInstallPackage(mgr, "dummy.package.1", "0.8.0");

            BuildAndInstallPackage(mgr, "package.id.1", "0.9.0");
            BuildAndInstallPackage(mgr, "package.id.1", "1.0.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(new TestLogger(), sourceNuGetRoot);

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
            string sourceNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.source");
            string targetNuGetRoot = TestUtils.CreateTestDirectory(this.TestContext, ".nuget.target");
            IPackageManager mgr = CreatePackageManager(sourceNuGetRoot);

            BuildAndInstallPackage(mgr, "package.id.1", "0.8.0");
            BuildAndInstallPackage(mgr, "package.id.1", "0.9.0-rc1");
            BuildAndInstallPackage(mgr, "package.id.1", "1.0.0");
            BuildAndInstallPackage(mgr, "package.id.1", "1.1.0-rc1");
            BuildAndInstallPackage(mgr, "dummy.package.1", "2.0.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(new TestLogger(), sourceNuGetRoot);

            // Act
            IPackage actual = handler.FetchPackage("package.id.1", null);

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

            BuildAndInstallPackage(mgr, "package.id.1", "0.9.0-rc1");
            BuildAndInstallPackage(mgr, "package.id.1", "1.0.0-rc1");
            BuildAndInstallPackage(mgr, "package.id.1", "1.1.0-rc1");
            BuildAndInstallPackage(mgr, "dummy.package.1", "2.0.0");
            BuildAndInstallPackage(mgr, "dummy.package.1", "2.0.0-rc2");

            NuGetPackageHandler handler = new NuGetPackageHandler(new TestLogger(), sourceNuGetRoot);

            // Act
            IPackage actual = handler.FetchPackage("package.id.1", null);

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

            BuildAndInstallPackage(mgr, "package.id.1", "0.8.0");
            BuildAndInstallPackage(mgr, "package.id.1", "0.9.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(new TestLogger(), sourceNuGetRoot);

            // 1. Package id not found
            IPackage actual = handler.FetchPackage("unknown.package.id", new SemanticVersion("0.8.0"));
            Assert.IsNull(actual, "Not expecting a package to be found");

            // 2. Package id not found
            actual = handler.FetchPackage("package.id.1", new SemanticVersion("0.7.0"));
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

        private ManifestMetadata GenerateTestMetadata(bool isReleased)
        {
            return new ManifestMetadata()
            {
                Authors = "Microsoft",
                Version = isReleased ? ReleaseVersion : PreReleaseVersion,
                Id = TestPackageName,
                Description = "A description",
            };
        }

        private ManifestMetadata GenerateTestMetadataWithDependency(bool isReleased, bool isDependencyReleased)
        {
            List<ManifestDependencySet> dependencies = new List<ManifestDependencySet>()
            {
                new ManifestDependencySet()
                {
                    Dependencies = new List<ManifestDependency>()
                    {
                        new ManifestDependency()
                        {
                            Id = TestPackageName,
                            Version = isDependencyReleased ? ReleaseVersion : PreReleaseVersion,
                        }
                    }
                }
            };
            return new ManifestMetadata()
            {
                Authors = "Microsoft",
                Version = isReleased ? ReleaseVersion : PreReleaseVersion,
                Id = DependentPackageName,
                Description = "A description",
                DependencySets = dependencies,
            };
        }

        private Stream BuildPackageToStream(ManifestMetadata metadata, Stream outputStream)
        {
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext, "source");
            string dummyTextFile = TestUtils.CreateTextFile(Guid.NewGuid().ToString(), testDir, "content");

            PackageBuilder packageBuilder = new PackageBuilder();

            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = dummyTextFile;
            file.TargetPath = "dummy.txt";
            packageBuilder.Files.Add(file);

            packageBuilder.Populate(metadata);
            packageBuilder.Save(outputStream);

            // Assert correct function of the above code when versions are specifically "Release" or "Prerelease"            
            if (String.Equals(metadata.Version, ReleaseVersion, StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsTrue(packageBuilder.IsReleaseVersion());
            }
            else if (String.Equals(metadata.Version, PreReleaseVersion, StringComparison.OrdinalIgnoreCase))
            {
                Assert.IsFalse(packageBuilder.IsReleaseVersion());
            }

            return outputStream;
        }

        private void BuildAndInstallPackage(IPackageManager manager, string id, string version)
        {
            ManifestMetadata metadata = GenerateTestMetadata(true);
            metadata.Id = id;
            metadata.Version = new SemanticVersion(version).ToString();

            Stream packageStream = BuildPackageToStream(metadata, new MemoryStream());
            packageStream.Position = 0;

            ZipPackage pkg = new ZipPackage(packageStream);
            manager.InstallPackage(pkg, true, true);
        }

        private void BuildAndSavePackage(ManifestMetadata metadata, string fileName)
        {
            string destinationName = fileName + "." + metadata.Version + ".nupkg";
            Stream fileStream = File.Open(destinationName, FileMode.OpenOrCreate);
            BuildPackageToStream(metadata, fileStream);
            fileStream.Dispose();
        }

        private void BuildTestPackages(bool isDependentPackageReleased, bool isTestPackageReleased)
        {
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext);

            string testPackageFile = Path.Combine(testDir, TestPackageName);
            string dependentPackageFile = Path.Combine(testDir, DependentPackageName);

            ManifestMetadata testMetadata = GenerateTestMetadata(isTestPackageReleased);
            BuildAndSavePackage(testMetadata, testPackageFile);

            ManifestMetadata dependentMetadata = GenerateTestMetadataWithDependency(isDependentPackageReleased, isTestPackageReleased);
            BuildAndSavePackage(dependentMetadata, dependentPackageFile);
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