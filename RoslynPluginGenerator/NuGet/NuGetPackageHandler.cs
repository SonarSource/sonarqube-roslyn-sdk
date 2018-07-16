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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NuGet;

namespace SonarQube.Plugins.Roslyn
{
    public class NuGetPackageHandler : INuGetPackageHandler
    {
        private readonly IPackageRepository remoteRepository;
        private readonly Common.ILogger logger;
        private readonly IPackageManager packageManager;

        public NuGetPackageHandler(IPackageRepository remoteRepository, string localCacheRoot, Common.ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(localCacheRoot))
            {
                throw new ArgumentNullException(nameof(localCacheRoot));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.remoteRepository = remoteRepository ?? throw new ArgumentNullException(nameof(remoteRepository));
            LocalCacheRoot = localCacheRoot;

            Directory.CreateDirectory(localCacheRoot);
            packageManager = new PackageManager(remoteRepository, localCacheRoot)
            {
                Logger = new NuGetLoggerAdapter(logger)
            };
        }

        #region INuGetPackageHandler

        public string LocalCacheRoot { get; }

        /// <summary>
        /// Attempts to download a NuGet package with the specified id and optional version
        /// to the specified directory
        /// </summary>
        public IPackage FetchPackage(string packageId, SemanticVersion version)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            IPackage package = TryGetPackage(remoteRepository, packageId, version);

            if (package != null)
            {
                try
                {
                    // Prerelease packages enabled by default
                    packageManager.InstallPackage(package, false, true, false);
                }
                catch (InvalidOperationException e)
                {
                    logger.LogError(UIResources.NG_ERROR_PackageInstallFail, e.Message);
                    return null;
                }
            }

            return package;
        }

        public IEnumerable<IPackage> GetInstalledDependencies(IPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            List<IPackage> dependencies = new List<IPackage>();
            GetAllDependencies(package, dependencies);

            return dependencies;
        }

        public string GetLocalPackageRootDirectory(IPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            Debug.Assert(packageManager.FileSystem != null);
            Debug.Assert(packageManager.PathResolver != null);
            string packageDirectory = packageManager.FileSystem.GetFullPath(packageManager.PathResolver.GetPackageDirectory(package));

            Debug.Assert(Directory.Exists(packageDirectory), "Expecting the package directory to exist: {0}", packageDirectory);
            return packageDirectory;
        }

        #endregion INuGetPackageHandler

        private IPackage TryGetPackage(IPackageRepository repository, string packageId, SemanticVersion packageVersion)
        {
            IPackage package = null;

            logger.LogInfo(UIResources.NG_LocatingPackages, packageId);
            IList<IPackage> packages = PackageRepositoryExtensions.FindPackagesById(repository, packageId).ToList();
            ListPackages(packages);

            if (packages.Count == 0)
            {
                logger.LogError(UIResources.NG_ERROR_PackageNotFound, packageId);
            }
            else
            {
                if (packageVersion == null)
                {
                    package = SelectLatestVersion(packages);
                }
                else
                {
                    package = packages.FirstOrDefault(p => p.Version == packageVersion);
                    if (package == null)
                    {
                        logger.LogError(UIResources.NG_ERROR_PackageVersionNotFound, packageVersion);
                    }
                }
            }

            return package;
        }

        private void ListPackages(IList<IPackage> packages)
        {
            logger.LogDebug(UIResources.NG_NumberOfPackagesLocated, packages.Count);

            if (packages.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(UIResources.NG_PackageVersionListHeader);
                foreach (IPackage package in packages)
                {
                    sb.AppendFormat("  {0}", package.Version);
                    if (package.IsLatestVersion)
                    {
                        sb.AppendFormat(" {0}", UIResources.NG_IsLatestPackageVersionSuffix);
                    }

                    sb.AppendLine();
                }
                logger.LogDebug(sb.ToString());
            }
        }

        private IPackage SelectLatestVersion(IList<IPackage> packages)
        {
            IPackage[] orderedPackages = packages.OrderBy(p => p.Version).ToArray();

            IPackage package = orderedPackages.LastOrDefault(p => p.IsLatestVersion);

            if (package == null)
            {
                package = packages.Last();
            }
            else
            {
                logger.LogDebug(UIResources.NG_UsingLatestPackageVersion);
            }
            Debug.Assert(package != null, "Failed to select a package");
            logger.LogInfo(UIResources.NG_SelectedPackageVersion, package.Version);

            return package;
        }

        private void GetAllDependencies(IPackage current, List<IPackage> collectedDependencies)
        {
            Debug.Assert(current != null);
            logger.LogDebug(UIResources.NG_ResolvingPackageDependencies, current.Id, current.Version);

            foreach (PackageDependency dependency in current.GetCompatiblePackageDependencies(null))
            {
                IPackage dependencyPackage = packageManager.LocalRepository.ResolveDependency(dependency, true, true);

                if (dependencyPackage == null)
                {
                    logger.LogWarning(UIResources.NG_FailedToResolveDependency, dependency.Id, dependency.VersionSpec.ToString());
                }
                else
                {
                    if (collectedDependencies.Contains(dependencyPackage))
                    {
                        logger.LogDebug(UIResources.NG_DuplicateDependency, dependencyPackage.Id, dependencyPackage.Version);
                    }
                    else
                    {
                        logger.LogDebug(UIResources.NG_AddingNewDependency, dependencyPackage.Id, dependencyPackage.Version);
                        collectedDependencies.Add(dependencyPackage);

                        GetAllDependencies(dependencyPackage, collectedDependencies);
                    }
                }
            }
        }
    }
}