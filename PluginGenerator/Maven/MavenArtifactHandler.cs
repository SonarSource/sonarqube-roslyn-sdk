//-----------------------------------------------------------------------
// <copyright file="MavenArtifactHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Plugins.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace SonarQube.Plugins.Maven
{
    public class MavenArtifactHandler : IMavenArtifactHandler
    {
        private const string LocalMavenDirectory = ".maven";
        private const string JAR_Extension = "jar";

        private readonly string localCacheDirectory;
        private readonly ILogger logger;

        public MavenArtifactHandler(ILogger logger)
            : this(null, logger)
        {
        }

        public MavenArtifactHandler(string localCacheDirectory, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.localCacheDirectory = localCacheDirectory;
            this.logger = logger;

            if (string.IsNullOrWhiteSpace(this.localCacheDirectory))
            {
                this.localCacheDirectory = Utilities.CreateTempDirectory(LocalMavenDirectory);
            }
        }

        public string LocalCacheDirectory { get { return this.localCacheDirectory; } }

        #region IMavenArtifactHandler interface

        public string FetchArtifactJarFile(IMavenCoordinate coordinate)
        {
            if (coordinate == null)
            {
                throw new ArgumentNullException("coordinate");
            }


            this.logger.LogDebug(MavenResources.MSG_ProcessingArtifact, coordinate);

            string filePath = TryGetJar(coordinate);

            return filePath;
        }

        #endregion

        #region Private methods
        
        private string TryGetJar(IMavenCoordinate coordinate)
        {
            Debug.Assert(coordinate != null, "Expecting a valid coordinate");
            Debug.Assert(!string.IsNullOrWhiteSpace(coordinate.Version));

            string localJarFilePath = this.GetFilePath(coordinate, JAR_Extension);

            if (File.Exists(localJarFilePath))
            {
                this.logger.LogDebug(MavenResources.MSG_UsingCachedFile, localJarFilePath);
            }
            else
            {
                string url = this.GetArtifactUrl(coordinate, JAR_Extension);
                this.DownloadFile(url, localJarFilePath);

                if (!File.Exists(localJarFilePath))
                {
                    localJarFilePath = null;
                }
            }
            return localJarFilePath;
        }


        private string GetArtifactUrl(IMavenCoordinate coordinate, string extension)
        {
            // Example url: https://repo1.maven.org/maven2/aopalliance/aopalliance/1.0/aopalliance-1.0.pom
            // i.e. [root]/[groupdId with "/" instead of "."]/[artifactId]/[version]/[artifactId]-[version].pom
            string url = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "https://repo1.maven.org/maven2/{0}/{1}/{2}/{1}-{2}.{3}",
                coordinate.GroupId.Replace(".", "/"),
                coordinate.ArtifactId,
                coordinate.Version,
                extension);
            return url;
        }

        /// <summary>
        /// Returns the path to the unique directory for the artifact
        /// </summary>
        private string GetArtifactFolderPath(IMavenCoordinate coordinate)
        {
            string path = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}\\{1}\\{2}",
                coordinate.GroupId.Replace(".", "\\"),
                coordinate.ArtifactId,
                coordinate.Version
                );

            path = Path.Combine(this.localCacheDirectory, path);
            return path;
        }

        private string GetFilePath(IMavenCoordinate coordinate, string extension)
        {
            string filePath = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}-{1}.{2}",
                coordinate.ArtifactId,
                coordinate.Version,
                extension);
            filePath = Path.Combine(this.GetArtifactFolderPath(coordinate), filePath);
            return filePath;
        }

        private void DownloadFile(string url, string localFilePath)
        {
            this.logger.LogDebug(MavenResources.MSG_DownloadingFile, url);
            Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));

            using (HttpClient httpClient = new HttpClient())
            using (HttpResponseMessage response = httpClient.GetAsync(url).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    using (FileStream fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
                    {
                        response.Content.CopyToAsync(fileStream).Wait();
                    }
                    this.logger.LogDebug(MavenResources.MSG_FileDownloaded, localFilePath);
                }
                else
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        this.logger.LogWarning(MavenResources.WARN_ArtifactWasNotFound, url);
                    }
                    else
                    {
                        this.logger.LogError(MavenResources.ERROR_FailedToDownloadArtifact, url, response.StatusCode, response.ReasonPhrase);
                    }
                }
            }

        }

        #endregion
    }
}
