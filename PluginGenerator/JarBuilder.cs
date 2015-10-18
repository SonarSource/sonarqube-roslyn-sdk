using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginGenerator
{
    public class JarBuilder
    {
        public const string MANIFEST_FILE_NAME = "MANIFEST.MF";

        private ILogger logger;

        private IDictionary<string, string> manifestProperties;

        private IDictionary<string, string> sourceFileToRelativePathMap;

        public JarBuilder(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.logger = logger;

            this.manifestProperties = new Dictionary<string, string>();
            this.sourceFileToRelativePathMap = new Dictionary<string, string>();
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
        /// <param name="fullPath"></param>
        /// <param name="relativeJarPath">The path and file name in the jar where the file will be written
        /// e.g. resources\rules.xml. If the argument is null the file will be added at the root level using
        /// the file name.</param>
        public void AddFile(string fullPath, string relativeJarPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentNullException("fullPath");
            }

            if (this.sourceFileToRelativePathMap.ContainsKey(fullPath))
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

            this.sourceFileToRelativePathMap.Add(fullPath, finalJarPath);
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

            this.WriteManifestFile(outputDirectory);

            this.WriteContentFiles(outputDirectory);
        }

        public void Build(string fullJarPath)
        {
            if (string.IsNullOrWhiteSpace(fullJarPath))
            {
                throw new ArgumentNullException("fullJarPath");
            }

            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            this.LayoutFiles(tempPath);

            CompileJarFile(fullJarPath, tempPath);
        }

        private string TryGetExistingJarPathKey(string relativePath)
        {
            KeyValuePair<string, string> existing = this.sourceFileToRelativePathMap.FirstOrDefault(k => k.Value.Equals(relativePath));
            return existing.Key;
        }

        private void WriteManifestFile(string outputDirectory)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in this.manifestProperties)
            {
                sb.AppendFormat(System.Globalization.CultureInfo.CurrentCulture,
                    "{0}={1}",
                    kvp.Key, kvp.Value);
                sb.AppendLine();
            }

            string fullPath = Path.Combine(outputDirectory, MANIFEST_FILE_NAME);
            File.WriteAllText(fullPath, sb.ToString());
        }

        private void WriteContentFiles(string outputDirectory)
        {
            foreach (KeyValuePair<string, string> sourceToPathEntry
                in this.sourceFileToRelativePathMap)
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

        private void CompileJarFile(string fullJarPath, string workingDir)
        {
            throw new NotImplementedException();
        }
    }
}
