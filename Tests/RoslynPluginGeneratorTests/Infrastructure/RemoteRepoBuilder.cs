/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
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

            // Retrieve and return the newly-created package
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
