using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Roslyn.SonarQube.PluginGenerator
{
    public class JarBuilder
    {
        public const string JAR_CONTENT_DIRECTORY_NAME = "jarContent";

        private readonly ILogger logger;
        private readonly IJdkWrapper jdkWrapper;

        private readonly IDictionary<string, string> manifestProperties;

        private readonly IDictionary<string, string> fileToRelativePathMap;


        private class JarFolders
        {
            private readonly string rootTempFolder;

            public JarFolders(string rootTempFolder)
            {
                this.rootTempFolder = rootTempFolder;
            }

            public string RootTempFolder { get { return this.rootTempFolder; } }
            public string ManifestFilePath {  get { return Path.Combine(this.rootTempFolder, JdkWrapper.MANIFEST_FILE_NAME); } }
            public string JarContentDirectory { get { return Path.Combine(this.rootTempFolder, JAR_CONTENT_DIRECTORY_NAME); } }
        }

        public JarBuilder(ILogger logger, IJdkWrapper jdkWrapper)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (jdkWrapper == null)
            {
                throw new ArgumentNullException("jdkWrapper");
            }

            this.logger = logger;
            this.jdkWrapper = jdkWrapper;

            this.manifestProperties = new Dictionary<string, string>();
            this.fileToRelativePathMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// Sets the value of a property in the manifest file.
        /// Any existing value will be overwritten.
        /// </summary>
        public void SetManifestPropety(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            this.manifestProperties[key] = value;
        }

        /// <summary>
        /// Adds a new file to the jar
        /// </summary>
        /// <param name="relativeJarPath">The path and file name in the jar where the file will be written
        /// e.g. resources\rules.xml. If the argument is null the file will be added at the root level using
        /// the file name.</param>
        public void AddFile(string fullPath, string relativeJarPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentNullException("fullPath");
            }

            if (this.fileToRelativePathMap.ContainsKey(fullPath))
            {
                throw new ArgumentException(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        UIResources.JarB_Error_FileAlreadySpecified,
                        fullPath));
            }

            string finalJarPath = string.IsNullOrWhiteSpace(relativeJarPath) ? Path.GetFileName(fullPath) : relativeJarPath;

            string existingKey = this.TryGetExistingJarPathKey(finalJarPath);
            if (existingKey != null)
            {
                throw new ArgumentException(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        UIResources.JarB_Error_RelativeJarPathAlreadySpecified,
                        finalJarPath,
                        existingKey));
            }

            this.fileToRelativePathMap.Add(fullPath, finalJarPath);
        }

        /// <summary>
        /// Writes the files to be embedded into the jar in the correct locations
        /// under the specified folder
        /// </summary>
        public void LayoutFiles(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentNullException("outputDirectory");
            }
            
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            JarFolders folders = new JarFolders(outputDirectory);

            this.WriteManifestFile(folders.ManifestFilePath);

            this.WriteContentFiles(folders.JarContentDirectory);
        }

        public bool Build(string fullJarPath)
        {
            if (string.IsNullOrWhiteSpace(fullJarPath))
            {
                throw new ArgumentNullException("fullJarPath");
            }

            if (!this.jdkWrapper.IsJdkInstalled())
            {
                throw new InvalidOperationException(UIResources.JarB_JDK_NotInstalled);
            }

            string tempPath = Path.Combine(Path.GetTempPath(), ".jarBuilder", Guid.NewGuid().ToString());

            JarFolders folders = new JarFolders(tempPath);
            this.LayoutFiles(tempPath);

            bool success = this.jdkWrapper.CompileJar(folders.JarContentDirectory, folders.ManifestFilePath, fullJarPath, this.logger);
            if (success)
            {
                this.logger.LogInfo(UIResources.JarB_JarBuiltSuccessfully, fullJarPath);
            }
            else
            {
                this.logger.LogInfo(UIResources.JarB_JarBuildingFailed);
            }
            return success;
        }

        private string TryGetExistingJarPathKey(string relativePath)
        {
            KeyValuePair<string, string> existing = this.fileToRelativePathMap.FirstOrDefault(k => k.Value.Equals(relativePath));
            return existing.Key;
        }

        private void WriteManifestFile(string manifestFilePath)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in this.manifestProperties)
            {
                sb.AppendFormat(System.Globalization.CultureInfo.CurrentCulture,
                    "{0}: {1}",
                    kvp.Key, kvp.Value);
                sb.AppendLine();
            }

            File.WriteAllText(manifestFilePath, sb.ToString());
        }

        private void WriteContentFiles(string outputDirectory)
        {
            foreach (KeyValuePair<string, string> sourceToPathEntry
                in this.fileToRelativePathMap)
            {
                if (!File.Exists(sourceToPathEntry.Key))
                {
                    throw new FileNotFoundException(UIResources.JarB_Error_FileNotFound, sourceToPathEntry.Key);
                }

                string fullOutputPath = Path.Combine(outputDirectory, sourceToPathEntry.Value);
                Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
                File.Copy(sourceToPathEntry.Key, fullOutputPath);
            }

        }

    }
}
