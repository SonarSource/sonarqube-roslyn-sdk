//-----------------------------------------------------------------------
// <copyright file="NuGetPackageHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using NuGet;
using SonarQube.Plugins.Common;
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

        public static IPackageRepository GetRepositoryFromConfigFiles(Common.ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            // Load the user and machine-wide settings
            logger.LogDebug(UIResources.NG_FetchingConfigFiles);

            NuGetMachineWideSettings machineSettings = new NuGetMachineWideSettings();
            ISettings settings = Settings.LoadDefaultSettings(null, null, machineSettings);
           
            // Get a package source provider that can use the settings
            PackageSourceProvider packageSourceProvider = new PackageSourceProvider(settings);

            logger.LogDebug(UIResources.NG_ListingEnablePackageSources);
            IEnumerable<PackageSource> enabledSources = packageSourceProvider.GetEnabledPackageSources();
            if (!enabledSources.Any())
            {
                logger.LogWarning(UIResources.NG_NoEnabledPackageSources);
            }
            else
            {
                foreach (PackageSource enabledSource in enabledSources)
                {
                    logger.LogDebug(UIResources.NG_ListEnabledPackageSource, enabledSource.Source, enabledSource.IsMachineWide);
                }
            }
            // Create an aggregate repository that uses all of the configured sources
            AggregateRepository aggRepo = packageSourceProvider.CreateAggregateRepository(PackageRepositoryFactory.Default, true /* ignore failing repos */ );

            return aggRepo;
        }

        public NuGetPackageHandler(Common.ILogger logger) : this(GetRepositoryFromConfigFiles(logger), Utilities.CreateTempDirectory(".nuget"), logger)
        {
        }

        public /* for test */ NuGetPackageHandler(IPackageRepository remoteRepository, string localPackageDestination, Common.ILogger logger)
        {
            if (remoteRepository == null)
            {
                throw new ArgumentNullException("remoteRepository");
            }
            if (string.IsNullOrWhiteSpace(localPackageDestination))
            {
                throw new ArgumentNullException("localPackageDestination");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.logger = logger;
            this.remoteRepository = remoteRepository;
            this.localCacheRoot = localPackageDestination;
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
                Directory.CreateDirectory(localCacheRoot);

                IPackageManager manager = new PackageManager(this.remoteRepository, localCacheRoot);
                manager.Logger = new NuGetLoggerAdapter(this.logger);

                try
                {
                    // Prerelease packages enabled by default
                    manager.InstallPackage(package, false, true, false);
                }
                catch (InvalidOperationException e)
                {
                    logger.LogError(UIResources.NG_ERROR_PackageInstallFail, e.Message);
                    return null;
                }
            }

            return package;
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
    }
}
