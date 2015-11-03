using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.SonarQube.Common;
using System.Text;

namespace Roslyn.SonarQube.AnalyzerPlugins
{
    public class NuGetPackageHandler
    {
        private readonly ILogger logger;

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

        private void DownloadPackage(NuGet.IPackage package, string downloadDirectory)
        {
            // Calling "GetFiles" will download the dlls to a temporary location
            // (somewhere under AppData\Local\Temp\nuget). We don't know exactly where,
            // so we need to unpack the files to a known location

            this.logger.LogInfo(UIResources.NG_DownloadingPackage);
            IList<NuGet.IPackageFile> files = package.GetFiles().ToList();
            this.logger.LogInfo(UIResources.NG_DownloadedPackage, files.Count);

            // Extract all of the files
            foreach (NuGet.IPackageFile file in files)
            {
                ExtractFile(file, downloadDirectory);
            }
        }

        private void ExtractFile(NuGet.IPackageFile file, string downloadDirectory)
        {
            // We're dumping all of the files into the same directory currently
            // to simplify that the assembly resolver code that attempts to reflect
            // on them. This might need to change e.g. if the extracted assemblies
            // include resource assemblies that need to be in a particular folder.
            byte[] buffer = new byte[4096];

            string fullFilePath = Path.Combine(downloadDirectory, Path.GetFileName(file.Path));
            this.logger.LogDebug(UIResources.NG_ExtractingFile, file.Path, fullFilePath);

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
