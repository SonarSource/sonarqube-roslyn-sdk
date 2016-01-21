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
        private readonly string packageSource;
        private readonly string localCacheRoot;
        private readonly Common.ILogger logger;

        private class NuGetLoggerAdapter : NuGet.ILogger
        {
            private readonly Common.ILogger logger;

            public NuGetLoggerAdapter(Common.ILogger logger)
            {
                if (logger == null)
                {
                    throw new ArgumentNullException("logger");
                }
                this.logger = logger;
            }

            public void Log(MessageLevel level, string message, params object[] args)
            {
                switch (level)
                {
                    case MessageLevel.Debug:
                        this.logger.LogDebug(message, args);
                        break;
                    case MessageLevel.Error:
                        this.logger.LogError(message, args);
                        break;
                    case MessageLevel.Warning:
                        this.logger.LogWarning(message, args);
                        break;
                    default:
                        this.logger.LogInfo(message, args);
                        break;
                }
            }

            public FileConflictResolution ResolveFileConflict(string message)
            {
                this.logger.LogInfo("NuGet: ResolveFileConflict: {0}", message);
                return FileConflictResolution.Ignore;
            }
        }

        public NuGetPackageHandler(string remotePackageSource, Common.ILogger logger) : this(remotePackageSource, Utilities.CreateTempDirectory(".nuget"), logger)
        {
        }

        public /* for test */ NuGetPackageHandler(string remotePackageSource, string localPackageDestination, Common.ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(remotePackageSource))
            {
                throw new ArgumentNullException("packageSource");
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
            this.packageSource = remotePackageSource;
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

            logger.LogDebug(UIResources.NG_CreatingRepository, this.packageSource);
            IPackageRepository repository = PackageRepositoryFactory.Default.CreateRepository(packageSource);

            IPackage package = TryGetPackage(repository, packageId, version);

            if (package != null)
            {
                Directory.CreateDirectory(localCacheRoot);

                IPackageManager manager = new PackageManager(repository, localCacheRoot);
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
