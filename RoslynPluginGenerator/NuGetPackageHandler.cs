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
    public class NuGetPackageHandler
    {
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

        public NuGetPackageHandler(Common.ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.logger = logger;
        }

        /// <summary>
        /// Attempts to download a NuGet package with the specified id and optional version
        /// to the specified directory
        /// </summary>
        public NuGet.IPackage FetchPackage(string packageSource, string packageId, SemanticVersion version, string downloadDirectory)
        {
            if (string.IsNullOrWhiteSpace(packageSource))
            {
                throw new ArgumentNullException("url");
            }
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException("packageId");
            }
            if (string.IsNullOrWhiteSpace(downloadDirectory))
            {
                throw new ArgumentNullException("downloadDirectory");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            logger.LogDebug(UIResources.NG_CreatingRepository, packageSource);
            IPackageRepository repository = PackageRepositoryFactory.Default.CreateRepository(packageSource);

            IPackage package = TryGetPackage(repository, packageId, version);

            if (package != null)
            {
                Directory.CreateDirectory(downloadDirectory);

                IPackageManager manager = new PackageManager(repository, downloadDirectory);
                manager.Logger = new NuGetLoggerAdapter(this.logger);

                try
                {
                    manager.InstallPackage(package, false, false, false);
                } catch (InvalidOperationException e)
                {
                    logger.LogError(UIResources.NG_ERROR_PackageInstallFail, e.Message);
                    return null;
                }
            }

            return package;
        }

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

        private void ListPackages(IList<NuGet.IPackage> packages)
        {
            logger.LogDebug(UIResources.NG_NumberOfPackagesLocated, packages.Count);

            if (packages.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(UIResources.NG_PackageVersionListHeader);
                foreach (NuGet.IPackage package in packages)
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

        private NuGet.IPackage SelectLatestVersion(IList<NuGet.IPackage> packages)
        {
            NuGet.IPackage package = packages.FirstOrDefault(p => p.IsLatestVersion);

            if (package == null)
            {
                package = packages.OrderBy(p => p.Version).Last();
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
