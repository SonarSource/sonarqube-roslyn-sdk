using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.SonarQube.Common;

namespace Roslyn.SonarQube.AnalyzerPlugins
{
    public class NuGetPackageHandler
    {
        private ILogger logger;

        public NuGetPackageHandler(ILogger logger)
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
        public NuGet.IPackage FetchPackage(string packageSource, string packageId, NuGet.SemanticVersion version, string downloadDirectory)
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

            NuGet.IPackage package = TryGetPackage(packageSource, packageId, version);

            if (package != null)
            {
                Directory.CreateDirectory(downloadDirectory);

                this.DownloadPackage(package, downloadDirectory);
            }

            return package;
        }

        private NuGet.IPackage TryGetPackage(string packageSource, string packageId, NuGet.SemanticVersion packageVersion)
        {
            NuGet.IPackage package = null;

            logger.LogDebug(UIResources.NG_CreatingRepository, packageSource);
            NuGet.IPackageRepository repository = NuGet.PackageRepositoryFactory.Default.CreateRepository(packageSource);

            logger.LogInfo(UIResources.NG_LocatingPackages, packageId);
            IList<NuGet.IPackage> packages = NuGet.PackageRepositoryExtensions.FindPackagesById(repository, packageId).ToList();

            logger.LogDebug(UIResources.NG_NumberOfPackagesLocated, packages.Count);
            if (packages.Count == 0)
            {
                logger.LogError(UIResources.NG_ERROR_PackageNotFound, packageId);
            }
            else
            {
                if (packageVersion == null)
                {
                    package = packages.FirstOrDefault(p => p.IsLatestVersion);
                    Debug.Assert(package != null, "Expecting latest package to exist");
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

        private void DownloadPackage(NuGet.IPackage package, string downloadDirectory)
        {
            IList<NuGet.IPackageFile> files = package.GetFiles().ToList();

            this.logger.LogInfo(UIResources.NG_DownloadingPackage, files.Count);

            // Download all of the files
            foreach (NuGet.IPackageFile file in files)
            {
                DownloadFile(file, downloadDirectory);
            }
        }

        private void DownloadFile(NuGet.IPackageFile file, string downloadDirectory)
        {
            byte[] buffer = new byte[4096];

            string fullFilePath = Path.Combine(downloadDirectory, Path.GetFileName(file.Path));
            this.logger.LogDebug(UIResources.NG_DownloadingFile, file.Path, fullFilePath);

            using (FileStream outputStream = File.OpenWrite(fullFilePath))
            using (Stream inputStream = file.GetStream())
            {
                int bytesRead = 0;
                do
                {
                    bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                    outputStream.Write(buffer, 0, bytesRead);
                } while (bytesRead > 0);
            }
        }
    }
}
