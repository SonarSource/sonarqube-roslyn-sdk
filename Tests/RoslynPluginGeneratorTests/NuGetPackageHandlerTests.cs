/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource Sàrl
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Test.Common;

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
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");

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
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");

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
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");

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
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");

            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);
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
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

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
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

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
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.8.0");
            BuildAndInstallPackage(remoteRepoBuilder, "package.id.1", "0.9.0");

            NuGetPackageHandler handler = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, targetNuGetRoot, new TestLogger());

            // 1. Package id not found
            IPackage actual = handler.FetchPackage("unknown.package.id", new SemanticVersion("0.8.0"));
            actual.Should().BeNull("Not expecting a package to be found");

            // 2. Package id not found
            actual = handler.FetchPackage("package.id.1", new SemanticVersion("0.7.0"));
            actual.Should().BeNull("Not expecting a package to be found");
        }

        [TestMethod]
        public void GetDependencies_DependenciesInstalledLocally_Succeeds()
        {
            // Arrange
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);
            string dummyContentFile = CreateDummyContentFile();

            // Build a dependency graph
            // A depends on B
            // B depends on C, D, E
            // D also depends on E => duplicate dependency
            // E depends on F
            // -> expected [B, C, D, E, F]

            // Leaf nodes
            IPackage f = remoteRepoBuilder.CreatePackage("f", "1.0", dummyContentFile, RemoteRepoBuilder.License.NotRequired /* no dependencies */);
            IPackage c = remoteRepoBuilder.CreatePackage("c", "1.0", dummyContentFile, RemoteRepoBuilder.License.NotRequired /* no dependencies */);

            IPackage e = remoteRepoBuilder.CreatePackage("e", "1.0", dummyContentFile, RemoteRepoBuilder.License.Required, f);
            IPackage d = remoteRepoBuilder.CreatePackage("d", "1.1", dummyContentFile, RemoteRepoBuilder.License.Required, e);

            IPackage b = remoteRepoBuilder.CreatePackage("b", "1.2", dummyContentFile, RemoteRepoBuilder.License.Required, c, d, e);

            IPackage a = remoteRepoBuilder.CreatePackage("a", "2.0", dummyContentFile, RemoteRepoBuilder.License.NotRequired, b);

            NuGetPackageHandler testSubject = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, targetNuGetRoot, new TestLogger());

            // We assume that all of the packages have been installed locally we attempt to resolve the dependencies
            testSubject.FetchPackage("a", new SemanticVersion("2.0"));

            // 1. Package with no dependencies -> empty list
            IEnumerable<IPackage> actualDependencies = testSubject.GetInstalledDependencies(f);
            AssertExpectedPackageIds(actualDependencies /* none */);

            // 2. Package with only direct dependencies -> non-empty list
            actualDependencies = testSubject.GetInstalledDependencies(e);
            AssertExpectedPackageIds(actualDependencies, "f");

            // 3. Package with indirect dependencies -> non-empty list
            actualDependencies = testSubject.GetInstalledDependencies(a);
            AssertExpectedPackageIds(actualDependencies, "b", "c", "d", "e", "f");
        }

        [TestMethod]
        public void GetDependencies_DependenciesNotInstalledLocally_Warning()
        {
            // Arrange
            string targetNuGetRoot = TestUtils.CreateTestDirectory(TestContext, ".nuget.target");
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);
            string dummyContentFile = CreateDummyContentFile();

            IEnumerable<IPackage> actualDependencies;

            // Build a dependency graph: "main" depends on "dependency"
            IPackage dependencyPackage = remoteRepoBuilder.CreatePackage("dependency.package.id", "1.2", dummyContentFile, RemoteRepoBuilder.License.Required /* no dependencies */);
            IPackage mainPackage = remoteRepoBuilder.CreatePackage("a", "2.0", dummyContentFile, RemoteRepoBuilder.License.NotRequired, dependencyPackage);

            // 1. Dependencies have not been installed locally -> warning but no error
            TestLogger logger = new TestLogger();
            NuGetPackageHandler testSubject = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, targetNuGetRoot, logger);
            actualDependencies = testSubject.GetInstalledDependencies(mainPackage);
            AssertExpectedPackageIds(actualDependencies /* no dependencies resolved*/);

            logger.AssertErrorsLogged(0);
            logger.AssertWarningsLogged(1);
            logger.AssertSingleWarningExists("dependency.package.id");

            // 2. Now install the package -> dependencies should resolve ok
            logger = new TestLogger();
            testSubject = new NuGetPackageHandler(remoteRepoBuilder.FakeRemoteRepo, targetNuGetRoot, logger);

            testSubject.FetchPackage(mainPackage.Id, mainPackage.Version);
            actualDependencies = testSubject.GetInstalledDependencies(mainPackage);
            AssertExpectedPackageIds(actualDependencies, "dependency.package.id");

            logger.AssertErrorsLogged(0);
            logger.AssertWarningsLogged(0);
        }

        #endregion Tests

        #region Private methods

        private void BuildAndInstallPackage(RemoteRepoBuilder remoteRepoBuilder, string id, string version)
        {
            string dummyTextFile = CreateDummyContentFile();
            remoteRepoBuilder.CreatePackage(id, version, dummyTextFile, RemoteRepoBuilder.License.NotRequired);
        }

        private IPackageRepository CreateTestPackageWithSingleDependency(string dependentPackageVersion, string testPackageVersion)
        {
            string dummyTextFile = CreateDummyContentFile();
            RemoteRepoBuilder remoteRepoBuilder = new RemoteRepoBuilder(TestContext);

            IPackage testPackage = remoteRepoBuilder.CreatePackage(TestPackageId, testPackageVersion, dummyTextFile, RemoteRepoBuilder.License.NotRequired);
            remoteRepoBuilder.CreatePackage(DependentPackageId, dependentPackageVersion, dummyTextFile, RemoteRepoBuilder.License.NotRequired, testPackage);

            return remoteRepoBuilder.FakeRemoteRepo;
        }

        private string CreateDummyContentFile()
        {
            string testDir = TestUtils.EnsureTestDirectoryExists(TestContext, "source");
            string dummyTextFile = TestUtils.CreateTextFile(Guid.NewGuid().ToString(), testDir, "content");
            return dummyTextFile;
        }

        #endregion Private methods

        #region Checks

        private static void AssertExpectedPackage(IPackage actual, string expectedId, string expectedVersion)
        {
            actual.Should().NotBeNull("The package should not be null");

            SemanticVersion sVersion = new SemanticVersion(expectedVersion);

            expectedId.Should().Be(actual.Id, "Unexpected package id");
            sVersion.Should().Be(actual.Version, "Unexpected package version");
        }

        private void AssertPackageDownloaded(string downloadDir, string expectedName, string expectedVersion)
        {
            string packageDir = Directory.GetDirectories(downloadDir)
                .SingleOrDefault(d => d.Contains(expectedName) && d.Contains(expectedVersion));
            packageDir.Should().NotBeNull("Expected a package to have been downloaded: " + expectedName);
        }

        private static void AssertExpectedPackageIds(IEnumerable<IPackage> actual, params string[] expectedIds)
        {
            foreach(string expectedId in expectedIds)
            {
                actual.Any(p => string.Equals(p.Id, expectedId, StringComparison.Ordinal)).Should().BeTrue("Dependency with the expected package id was not found. Id: {0}", expectedId);
            }
            expectedIds.Length.Should().Be(actual.Count(), "Too many dependencies returned");
        }

        #endregion Checks
    }
}