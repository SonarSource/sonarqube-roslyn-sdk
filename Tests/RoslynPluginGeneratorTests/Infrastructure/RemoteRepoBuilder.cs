//-----------------------------------------------------------------------
// <copyright file="RemoteRepoBuilder.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Test.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    /// <summary>
    /// Helper class to simplify building a dummy remote repository
    /// containing packages
    /// </summary>
    internal class RemoteRepoBuilder
    {
        public enum License { Required, NotRequired };

        private readonly TestContext testContext;
        private readonly PackageManager manager;
        private readonly IPackageRepository fakeRemoteRepo;

        public RemoteRepoBuilder(TestContext testContext)
        {
            if (testContext == null)
            {
                throw new ArgumentNullException("testContext");
            }
            this.testContext = testContext;

            string packageSource = GetFakeRemoteNuGetSourceDir();
            this.fakeRemoteRepo = PackageRepositoryFactory.Default.CreateRepository(packageSource);
            this.manager = new PackageManager(this.fakeRemoteRepo, packageSource);
        }

        public IPackageRepository FakeRemoteRepo
        {
            get
            {
                return this.fakeRemoteRepo;
            }
        }

        public void AddPackage(IPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.manager.InstallPackage(package, true, true);
        }

        public IPackage CreatePackageXXX(
            string packageId,
            string packageVersion,
            string contentFilePath,
            License requiresLicenseAccept)
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

            builder.Populate(metadata);

            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = contentFilePath;
            file.TargetPath = Path.GetFileName(contentFilePath);
            builder.Files.Add(file);

            string fileName = packageId.ToString() + "." + metadata.Version + ".nupkg";
            string destinationName = Path.Combine(this.manager.LocalRepository.Source, fileName);

            using (Stream fileStream = File.Open(destinationName, FileMode.OpenOrCreate))
            {
                builder.Save(fileStream);
            }

            // Retrieve and return the newly-created pacakge
            IPackage package = this.fakeRemoteRepo.FindPackage(packageId, new SemanticVersion(packageVersion));
            Assert.IsNotNull(package, "Test setup error: failed to create and retrieve a test package");

            return package;
        }

        public IPackage CreatePackage(
            string packageId,
            string packageVersion,
            string contentFilePath,
            License requiresLicenseAccept,
            params IPackage[] dependencies)
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
            foreach (IPackage dependencyNode in dependencies)
            {
                dependencyList.Add(new ManifestDependency()
                {
                    Id = dependencyNode.Id,
                    Version = dependencyNode.Version.ToString(),
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

            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = contentFilePath;
            file.TargetPath = Path.GetFileName(contentFilePath);
            builder.Files.Add(file);

            string fileName = packageId + "." + metadata.Version + ".nupkg";
            string destinationName = Path.Combine(this.manager.LocalRepository.Source, fileName);

            using (Stream fileStream = File.Open(destinationName, FileMode.OpenOrCreate))
            {
                builder.Save(fileStream);
            }

            // Retrieve and return the newly-created pacakge
            IPackage package = this.fakeRemoteRepo.FindPackage(packageId, new SemanticVersion(packageVersion));
            Assert.IsNotNull(package, "Test setup error: failed to create and retrieve a test package");

            return package;
        }


        private string GetFakeRemoteNuGetSourceDir()
        {
            return TestUtils.EnsureTestDirectoryExists(this.testContext, ".fakeRemoteNuGet");
        }
    }
}
