//-----------------------------------------------------------------------
// <copyright file="NuGetPackageHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using NuGet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SonarQube.Plugins.Roslyn
{
    public class NuGetPackageHandler : INuGetPackageHandler
    {
        private readonly IPackageRepository remoteRepository;
        private readonly string localCacheRoot;
        private readonly Common.ILogger logger;
        private readonly IPackageManager packageManager;

        public NuGetPackageHandler(IPackageRepository remoteRepository, string localCacheRoot, Common.ILogger logger)
        {
            if (remoteRepository == null)
            {
                throw new ArgumentNullException("remoteRepository");
            }
            if (string.IsNullOrWhiteSpace(localCacheRoot))
            {
                throw new ArgumentNullException("localPackageDestination");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.logger = logger;
            this.remoteRepository = remoteRepository;
            this.localCacheRoot = localCacheRoot;

            Directory.CreateDirectory(localCacheRoot);
            this.packageManager = new PackageManager(remoteRepository, localCacheRoot);
            this.packageManager.Logger = new NuGetLoggerAdapter(logger);
        }

        #region INuGetPackageHandler

        public string LocalCacheRoot
        {
            get
            {
                return localCacheRoot;
            }
        }

        /// <summary>
        /// Attempts to download a NuGet package with the specified id and optional version
        /// to the specified directory
        /// </summary>
        public IPackage FetchPackage(string packageId, SemanticVersion version)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException("packageId");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            IPackage package = TryGetPackage(this.remoteRepository, packageId, version);

            if (package != null)
            {
                try
                {
                    // Prerelease packages enabled by default
                    this.packageManager.InstallPackage(package, false, true, false);
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
                throw new ArgumentNullException("package");
            }

            List<IPackage> dependencies = new List<IPackage>();
            GetAllDependencies(package, dependencies);

            return dependencies;
        }

        public string GetLocalPackageRootDirectory(IPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            Debug.Assert(this.packageManager.FileSystem != null);
            Debug.Assert(this.packageManager.PathResolver != null);
            string packageDirectory = this.packageManager.FileSystem.GetFullPath(this.packageManager.PathResolver.GetPackageDirectory(package));

            Debug.Assert(Directory.Exists(packageDirectory), "Expecting the package directory to exist: {0}", packageDirectory);
            return packageDirectory;
        }

        #endregion

        private IPackage TryGetPackage(IPackageRepository repository, string packageId, SemanticVersion packageVersion)
        {
            IPackage package = null;

            logger.LogInfo(UIResources.NG_LocatingPackages, packageId);
            IList<IPackage> packages = PackageRepositoryExtensions.FindPackagesById(repository, packageId).ToList();
            this.ListPackages(packages);

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
                this.logger.LogDebug(sb.ToString());
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
                this.logger.LogDebug(UIResources.NG_UsingLatestPackageVersion);
            }
            Debug.Assert(package != null, "Failed to select a package");
            logger.LogInfo(UIResources.NG_SelectedPackageVersion, package.Version);

            return package;
        }

        private void GetAllDependencies(IPackage current, List<IPackage> collectedDependencies)
        {
            Debug.Assert(current != null);
            this.logger.LogDebug(UIResources.NG_ResolvingPackageDependencies, current.Id, current.Version);

            foreach (PackageDependency dependency in current.GetCompatiblePackageDependencies(null))
            {
                IPackage dependencyPackage = this.packageManager.LocalRepository.ResolveDependency(dependency, true, true);

                if (dependencyPackage == null)
                {
                    this.logger.LogWarning(UIResources.NG_FailedToResolveDependency, dependency.Id, dependency.VersionSpec.ToString());
                }
                else
                {
                    if (collectedDependencies.Contains(dependencyPackage))
                    {
                        this.logger.LogDebug(UIResources.NG_DuplicateDependency, dependencyPackage.Id, dependencyPackage.Version);
                    }
                    else
                    {
                        this.logger.LogDebug(UIResources.NG_AddingNewDependency, dependencyPackage.Id, dependencyPackage.Version);
                        collectedDependencies.Add(dependencyPackage);

                        GetAllDependencies(dependencyPackage, collectedDependencies);
                    }
                }
            }
        }

    }
}
