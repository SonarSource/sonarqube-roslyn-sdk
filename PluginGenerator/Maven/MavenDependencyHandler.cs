//-----------------------------------------------------------------------
// <copyright file="MavenDependencyHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SonarQube.Plugins.Maven
{
    // TODO:
    // * inherited dependencies
    // * parsing version ranges e.g. [1.0.0,)
    // * version conflict resolution
    // * checking for duplicates
    // * exclusions
    public class MavenDependencyHandler : IMavenDependencyHandler
    {
        private const string LocalMavenDirectory = ".maven";
        private const string POM_Extension = "pom";
        private const string JAR_Extension = "jar";

        private readonly string localCacheDirectory;
        private readonly ILogger logger;

        /// <summary>
        /// Maps coords to the corresponding POM
        /// </summary>
        private Dictionary<MavenCoordinate, MavenPartialPOM> coordPomMap;

        public MavenDependencyHandler(ILogger logger)
            : this(null, logger)
        {
        }

        public MavenDependencyHandler(string localCacheDirectory, ILogger logger)
        {
            if (logger == null) { throw new ArgumentNullException("logger"); }

            this.localCacheDirectory = localCacheDirectory;
            this.logger = logger;

            if (string.IsNullOrWhiteSpace(this.localCacheDirectory))
            {
                this.localCacheDirectory = Utilities.CreateTempDirectory(LocalMavenDirectory);
            }

            this.coordPomMap = new Dictionary<MavenCoordinate, MavenPartialPOM>();
        }

        public string LocalCacheDirectory { get { return this.localCacheDirectory; } }

        #region IMavenDependencyHandler interface

        public IEnumerable<string> GetJarFiles(MavenCoordinate coordinate, bool includeDependencies)
        {
            if (coordinate == null) { throw new ArgumentNullException("descriptor"); }

            this.logger.LogDebug(MavenResources.MSG_ProcessingDependency, coordinate);

            List<string> jarFiles = new List<string>();
            List<MavenCoordinate> visited = new List<MavenCoordinate>(); // guard against recursion
            this.GetJarFiles(coordinate, includeDependencies, jarFiles, visited);

            return jarFiles;
        }

        #endregion

        #region Private methods

        private void GetJarFiles(MavenCoordinate coordinate, bool includeDependencies, List<string> files, List<MavenCoordinate> visited)
        {
            if (visited.Contains(coordinate))
            {
                this.logger.LogDebug(MavenResources.MSG_DependencyAlreadyVisited, coordinate);
                return;
            }
            visited.Add(coordinate);

            Debug.Assert(!string.IsNullOrWhiteSpace(coordinate.Version));

            MavenPartialPOM pom = this.TryGetPOM(coordinate);
            if (pom == null)
            {
                return; // failed to retrieve the POM for the artifact
            }

            if (pom.Packaging == "jar" || pom.Packaging == null)
            {
                string localJarFilePath = TryGetJar(coordinate);

                if (localJarFilePath != null)
                {
                    if (files.Contains(localJarFilePath, StringComparer.OrdinalIgnoreCase))
                    {
                        this.logger.LogWarning(MavenResources.WARN_JarAddedByAnotherDependency, coordinate, localJarFilePath);
                    }
                    else
                    {
                        files.Add(localJarFilePath);
                    }
                }
            }
            else
            {
                this.logger.LogDebug(MavenResources.MSG_POMDoesNotContainAJar, pom, pom.Packaging);
            }

            if (includeDependencies)
            {
                FetchDependencies(files, visited, pom);
            }
        }

        private void FetchDependencies(List<string> files, List<MavenCoordinate> visited, MavenPartialPOM pom)
        {
            foreach (MavenDependency dependency in this.GetAllDependencies(pom))
            {
                if (ShouldIncludeDependency(dependency))
                {
                    string resolvedVersion = this.ResolveDependencyVersion(dependency, pom);

                    if (resolvedVersion == null)
                    {
                        logger.LogWarning(MavenResources.WARN_FailedToResolveDependency, dependency);
                    }
                    else
                    {
                        MavenDependency resolvedDependency = new MavenDependency(dependency.GroupId, dependency.ArtifactId, resolvedVersion);
                        this.GetJarFiles(resolvedDependency, true, files, visited);
                    }
                }
                else
                {
                    this.logger.LogDebug(MavenResources.MSG_SkippingScopedDependency, dependency, dependency.Scope);
                }
            }
        }

        private string GetArtifactUrl(MavenCoordinate coordinate, string extension)
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
        private string GetArtifactFolderPath(MavenCoordinate coordinate)
        {
            string path = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}/{1}/{2}",
                coordinate.GroupId.Replace(".", "/"),
                coordinate.ArtifactId,
                coordinate.Version
                );

            path = Path.Combine(path, this.localCacheDirectory);
            return path;
        }

        private string GetFilePath(MavenCoordinate coordinate, string extension)
        {
            string filePath = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}-{1}.{2}",
                coordinate.ArtifactId,
                coordinate.Version,
                extension);
            filePath = Path.Combine(this.GetArtifactFolderPath(coordinate), filePath);
            return filePath;
        }

        private string TryGetJar(MavenCoordinate coordinate)
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

        private MavenPartialPOM TryGetPOM(MavenCoordinate coordinate)
        {
            Debug.Assert(coordinate != null, "Expecting a valid coordinate");
            Debug.Assert(!string.IsNullOrWhiteSpace(coordinate.Version));

            MavenPartialPOM pom;

            // See if we have already loaded this pom
            if (this.coordPomMap.TryGetValue(coordinate, out pom))
            {
                return pom;
            }

            string localPOMFilePath = this.GetFilePath(coordinate, POM_Extension);

            if (File.Exists(localPOMFilePath))
            {
                this.logger.LogDebug(MavenResources.MSG_UsingCachedFile, localPOMFilePath);
                pom = MavenPartialPOM.Load(localPOMFilePath);
            }
            else
            {
                pom = DownloadPOM(coordinate);
            }

            this.coordPomMap[coordinate] = pom; // cache the result to avoid further lookups
            return pom;
        }

        private MavenPartialPOM DownloadPOM(MavenCoordinate descriptor)
        {
            string url = this.GetArtifactUrl(descriptor, POM_Extension);
            string localPOMFilePath = this.GetFilePath(descriptor, POM_Extension);
            this.DownloadFile(url, localPOMFilePath);

            MavenPartialPOM pomFile = null;
            if (File.Exists(localPOMFilePath))
            {
                pomFile = MavenPartialPOM.Load(localPOMFilePath);
            }
            return pomFile;
        }

        /// <summary>
        /// Returns all of dependencies for the specified co-ordinate, including
        /// inherited dependencies
        /// </summary>
        private IEnumerable<MavenDependency> GetAllDependencies(MavenPartialPOM pom)
        {
            List<MavenDependency> allDependencies = new List<MavenDependency>();
            while(pom != null)
            {
                this.logger.LogDebug(MavenResources.MSG_AddingDependenciesForPOM, pom);
                AddDependencies(pom.Dependencies, allDependencies);

                if (pom.Parent != null)
                {
                    pom = this.TryGetPOM(pom.Parent);
                }
                else
                {
                    pom = null;
                }
            }

            return allDependencies;
        }

        private void AddDependencies(IEnumerable<MavenDependency> source, List<MavenDependency> target)
        {
            foreach(MavenDependency sourceItem in source)
            {
                // Ignore inherited artifacts that are already in the list
                if (ContainsArtifact(target, sourceItem))
                {
                    this.logger.LogDebug(MavenResources.MSG_SkippingInheritedDependency, sourceItem);
                }
                else
                {
                    target.Add(sourceItem);
                }
            }
        }

        private static bool ContainsArtifact(IEnumerable<MavenCoordinate> coords, MavenCoordinate item)
        {
            return coords.Any(c => MavenCoordinate.IsSameArtifact(item, c));
        }

        private static bool ShouldIncludeDependency(MavenDependency dependency)
        {
            string[] scopesToInclude = { null, "", "compile", "runtime" };

            bool include = scopesToInclude.Any(s => string.Equals(dependency.Scope, s, MavenPartialPOM.PomComparisonType));

            return include;
        }

        private string ResolveDependencyVersion(MavenDependency dependency, MavenPartialPOM currentPom)
        {
            string effectiveVersion = ExpandVariables(dependency.Version, currentPom);

            if (dependency.Version == null)
            {
                effectiveVersion = this.TryGetVersionFromDependencyManagement(dependency, currentPom);
            }

            if (effectiveVersion == null && currentPom.Parent != null)
            {
                this.logger.LogDebug(MavenResources.MSG_AttemptingToResolveFromParentPOM, currentPom.Parent);
                MavenPartialPOM parentPOM = this.TryGetPOM(currentPom.Parent);
                if (parentPOM != null)
                {
                    effectiveVersion = ResolveDependencyVersion(dependency, parentPOM);

                    if (effectiveVersion != null)
                    {
                        logger.LogDebug(MavenResources.MSG_ResolvedVersionInPom, parentPOM);
                    }
                }
            }

            return effectiveVersion;
        }

        private string ExpandVariables(string rawValue, MavenPartialPOM pom)
        {
            if (rawValue == null) { return null; }

            string expandedValue = rawValue;

            // Match strings "${xxx}" and extract the "xxx"
            Match match = Regex.Match(rawValue, "\\A\\${([\\S]+)}$");
            if (match.Success)
            {
                // Try to resolve the variable
                Debug.Assert(match.Groups.Count == 2);
                string variable = match.Groups[1].Value;

                if (string.Equals("project.version", variable, MavenPartialPOM.PomComparisonType) ||
                    // Support the obsolete variable formats
                    string.Equals("pom.version", variable, MavenPartialPOM.PomComparisonType) ||
                    string.Equals("version", variable, MavenPartialPOM.PomComparisonType))
                {
                    this.logger.LogDebug(MavenResources.MSG_ExpandedProjectVariable, rawValue);
                    expandedValue = pom.Version;
                }
                else if (pom.Properties != null && pom.Properties.ContainsKey(variable))
                {
                    this.logger.LogDebug(MavenResources.MSG_ExpandedProjectVariable, rawValue);
                    expandedValue = pom.Properties[variable];
                }
                else
                {
                    this.logger.LogWarning(MavenResources.WARN_UnrecognizedProjectVariable, rawValue);
                    expandedValue = null;
                }
            }

            return expandedValue;
        }

        private string TryGetVersionFromDependencyManagement(MavenDependency dependency, MavenPartialPOM pom)
        {
            Debug.Assert(dependency.Version == null);

            string effectiveVersion = null;
            if (pom.DependencyManagement != null && pom.DependencyManagement.Dependencies != null)
            {
                MavenDependency match = pom.DependencyManagement.Dependencies.FirstOrDefault(d =>
                    string.Equals(dependency.GroupId, d.GroupId, MavenPartialPOM.PomComparisonType) &&
                    string.Equals(dependency.ArtifactId, d.ArtifactId, MavenPartialPOM.PomComparisonType)
                );

                if (match != null)
                {
                    effectiveVersion = ExpandVariables(match.Version, pom);
                    this.logger.LogDebug(MavenResources.MSG_ResolvedVersionFromDependencyManagement, effectiveVersion);
                }
            }
            return effectiveVersion;
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
                        this.logger.LogWarning(MavenResources.WARN_DependencyWasNotFound, url);
                    }
                    else
                    {
                        this.logger.LogError(MavenResources.ERROR_FailedToDownloadDependency, url, response.StatusCode, response.ReasonPhrase);
                    }
                }
            }

        }

        #endregion
    }
}
