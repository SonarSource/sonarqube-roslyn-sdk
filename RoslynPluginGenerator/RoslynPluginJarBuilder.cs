//-----------------------------------------------------------------------
// <copyright file="RoslynPluginJarBuilder.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Creates a SonarQube rules plugin
    /// </summary>
    public class RoslynPluginJarBuilder
    {
        /// <summary>
        /// Name of the embedded resource that contains the .jar file to update
        /// </summary>
        private const string EmptyTemplateJarResourceName = "SonarQube.Plugins.Roslyn.Resources.sonar-roslyn-sdk-template-plugin-1.0-empty.jar";

        /// <summary>
        /// The name of the plugin class in the embedded jar file
        /// </summary>
        private const string PluginClassName = "org.sonar.plugins.roslynsdk.RoslynSdkGeneratedPlugin";


        // Locations in the jar where various file should be embedded
        private const string RelativeManifestResourcePath = "META-INF\\MANIFEST.MF";
        private const string RelativeConfigurationResourcePath = "org\\sonar\\plugins\\roslynsdk\\configuration.xml";
        private const string RelativeRulesXmlResourcePath = "org\\sonar\\plugins\\roslynsdk\\rules.xml";
        private const string RelativeSqaleXmlResourcePath = "org\\sonar\\plugins\\roslynsdk\\sqale.xml";

        private readonly ILogger logger;

        private readonly IDictionary<string, string> pluginProperties;
        private readonly JarManifestBuilder jarManifestBuilder;
        private readonly IDictionary<string, string> fileToRelativePathMap;
        private string language;
        private string rulesFilePath;
        private string sqaleFilePath;
        private string repositoryKey;
        private string repositoryName;

        private string outputJarFilePath;

        #region Public methods

        public RoslynPluginJarBuilder(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;

            this.pluginProperties = new Dictionary<string, string>();
            this.jarManifestBuilder = new JarManifestBuilder();
            this.fileToRelativePathMap = new Dictionary<string, string>();

            this.SetFixedManifestProperties();
        }

        public RoslynPluginJarBuilder SetJarFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            this.outputJarFilePath = filePath;

            return this;
        }

        /// <summary>
        /// Sets a SonarQube plugin property (i.e. one that will be exported by the plugin)
        /// </summary>
        public RoslynPluginJarBuilder SetPluginProperty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.pluginProperties[name] = value;
            return this;
        }

        /// <summary>
        /// Sets a property that will appear in the manifest file
        /// </summary>
        public RoslynPluginJarBuilder SetManifestProperty(string name, string value)
        {
            this.jarManifestBuilder.SetProperty(name, value);
            return this;
        }

        public RoslynPluginJarBuilder SetPluginManifestProperties(PluginManifest definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            SetNonNullManifestProperty(WellKnownPluginProperties.License, definition.License);
            SetNonNullManifestProperty(WellKnownPluginProperties.OrganizationUrl, definition.OrganizationUrl);
            SetNonNullManifestProperty(WellKnownPluginProperties.Version, definition.Version);
            SetNonNullManifestProperty(WellKnownPluginProperties.Homepage, definition.Homepage);
            SetNonNullManifestProperty(WellKnownPluginProperties.SourcesUrl, definition.SourcesUrl);
            SetNonNullManifestProperty(WellKnownPluginProperties.Developers, definition.Developers);
            SetNonNullManifestProperty(WellKnownPluginProperties.IssueTrackerUrl, definition.IssueTrackerUrl);
            SetNonNullManifestProperty(WellKnownPluginProperties.TermsAndConditionsUrl, definition.TermsConditionsUrl);
            SetNonNullManifestProperty(WellKnownPluginProperties.OrganizationName, definition.Organization);
            SetNonNullManifestProperty(WellKnownPluginProperties.PluginName, definition.Name);
            SetNonNullManifestProperty(WellKnownPluginProperties.Description, definition.Description);

            string key = definition.Key;
            PluginKeyUtilities.ThrowIfInvalid(key);
            this.SetManifestProperty(WellKnownPluginProperties.Key, key);

            return this;
        }

        private void SetNonNullManifestProperty(string property, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                this.SetManifestProperty(property, value);
            }
        }

        /// <summary>
        /// Adds a file to the jar. The location of the file in the jar
        /// is specified by the <paramref name="relativeJarPath"/>.
        /// </summary>
        public RoslynPluginJarBuilder AddResourceFile(string fullFilePath, string relativeJarPath)
        {
            if (string.IsNullOrWhiteSpace(fullFilePath))
            {
                throw new ArgumentNullException(nameof(fullFilePath));
            }

            this.fileToRelativePathMap[fullFilePath] = relativeJarPath;

            return this;
        }

        public RoslynPluginJarBuilder SetLanguage(string ruleLanguage)
        {
            // This is a general-purpose rule plugin builder i.e.
            // it's not limited to C# or VB, so we can only check that the 
            // supplied language isn't null/empty.
            if (string.IsNullOrWhiteSpace(ruleLanguage))
            {
                throw new ArgumentNullException(nameof(ruleLanguage));
            }
            this.language = ruleLanguage;
            return this;
        }

        public RoslynPluginJarBuilder SetRulesFilePath(string filePath)
        {
            // The existence of the file will be checked before building
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            this.rulesFilePath = filePath;
            return this;
        }

        public RoslynPluginJarBuilder SetSqaleFilePath(string filePath)
        {
            // The existence of the file will be checked before building
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            this.sqaleFilePath = filePath;
            return this;
        }

        public RoslynPluginJarBuilder SetRepositoryKey(string key)
        {
            RepositoryKeyUtilities.ThrowIfInvalid(key);
            this.repositoryKey = key;
            return this;
        }

        public RoslynPluginJarBuilder SetRepositoryName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.repositoryName = name;
            return this;
        }

        /// <summary>
        /// Compiles the source files that have been supplied and builds the jar file
        /// </summary>
        public void Build()
        {
            // Temp working folder
            string tempWorkingDir = Utilities.CreateTempDirectory(".builder");
            tempWorkingDir = Utilities.CreateSubDirectory(tempWorkingDir, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempWorkingDir);

            if (File.Exists(this.JarFilePath))
            {
                this.logger.LogWarning(UIResources.Builder_ExistingJarWillBeOvewritten);
            }

            this.BuildJar(tempWorkingDir);
        }

        public string JarFilePath { get { return this.outputJarFilePath; } }

        #endregion

        #region Private methods configuration

        public void BuildJar(string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentNullException(nameof(workingDirectory));
            }

            this.ValidateConfiguration();

            // Create the config and manifest files
            string configFilePath = BuildConfigFile(workingDirectory);
            string manifestFilePath = this.jarManifestBuilder.WriteManifest(workingDirectory);

            // Update the jar
            string templateJarFilePath = ExtractTemplateJarFile(workingDirectory);
            ArchiveUpdater updater = new ArchiveUpdater(workingDirectory, this.logger);

            updater.SetInputArchive(templateJarFilePath)
                .SetOutputArchive(this.outputJarFilePath)
                .AddFile(manifestFilePath, RelativeManifestResourcePath)
                .AddFile(configFilePath, RelativeConfigurationResourcePath)
                .AddFile(this.rulesFilePath, RelativeRulesXmlResourcePath);

            foreach(KeyValuePair<string, string> kvp in this.fileToRelativePathMap)
            {
                updater.AddFile(kvp.Key, kvp.Value);
            }

            if (!string.IsNullOrWhiteSpace(this.sqaleFilePath))
            {
                updater.AddFile(this.sqaleFilePath, RelativeSqaleXmlResourcePath);
            }

            updater.UpdateArchive();
        }

        private void ValidateConfiguration()
        {
            // TODO: validate other inputs
            this.CheckPropertyIsSet(WellKnownPluginProperties.PluginName);
            string key = this.CheckPropertyIsSet(WellKnownPluginProperties.Key);
            PluginKeyUtilities.ThrowIfInvalid(key);

            if (string.IsNullOrWhiteSpace(this.JarFilePath))
            {
                throw new InvalidOperationException(UIResources.Builder_Error_OutputJarPathMustBeSpecified);
            }
        }

        private string CheckPropertyIsSet(string propertyName)
        {
            string value;
            this.jarManifestBuilder.TryGetValue(propertyName, out value);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    UIResources.Builder_Error_RequiredPropertyMissing, propertyName));
            }
            return value;
        }

        private string BuildConfigFile(string workingDirectory)
        {
            string configFilePath = Path.Combine(workingDirectory, "config.xml");

            RoslynSdkConfiguration config = new RoslynSdkConfiguration();

            config.PluginKeyDifferentiator = this.FindPluginKey();

            config.RepositoryKey = this.repositoryKey;
            config.RepositoryName = this.repositoryName;
            config.RepositoryLanguage = this.language;
            config.RulesXmlResourcePath = GetAbsoluteResourcePath(RelativeRulesXmlResourcePath);

            if (!string.IsNullOrWhiteSpace(this.sqaleFilePath))
            {
                config.SqaleXmlResourcePath = GetAbsoluteResourcePath(RelativeSqaleXmlResourcePath);
            }

            foreach(KeyValuePair<string,string> kvp in this.pluginProperties)
            {
                config.Properties[kvp.Key] = kvp.Value;
            }

            config.Save(configFilePath);
            return configFilePath;
        }

        private static string GetAbsoluteResourcePath(string relativeFilePath)
        {
            return "/" + relativeFilePath.Replace("\\", "/");
        }

        private string FindPluginKey()
        {
            string pluginKey;
            this.jarManifestBuilder.TryGetValue(WellKnownPluginProperties.Key, out pluginKey);
            if (pluginKey != null)
            {
                pluginKey = PluginKeyUtilities.GetValidKey(pluginKey);
            }
            return pluginKey;
        }
        
        /// <summary>
        /// Sets the invariant, required manifest properties
        /// </summary>
        private void SetFixedManifestProperties()
        {
            // This property must appear first in the manifest.
            // See http://docs.oracle.com/javase/6/docs/technotes/guides/jar/jar.html#JAR%20Manifest
            this.jarManifestBuilder.SetProperty("Sonar-Version", "4.5.2");
            this.jarManifestBuilder.SetProperty("Plugin-Dependencies", "META-INF/lib/sslr-squid-bridge-2.6.jar");
            this.jarManifestBuilder.SetProperty("Plugin-SourcesUrl", "https://github.com/SonarSource-VisualStudio/sonarqube-roslyn-sdk-template-plugin");

            this.jarManifestBuilder.SetProperty("Plugin-Class", PluginClassName);
        }

        private static string ExtractTemplateJarFile(string workingDirectory)
        {
            string templateJarFilePath = Path.Combine(workingDirectory, "template.jar");

            using (Stream resourceStream = typeof(RoslynPluginJarBuilder).Assembly.GetManifestResourceStream(EmptyTemplateJarResourceName))
            {
                using (FileStream file = new FileStream(templateJarFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    resourceStream.CopyTo(file);
                    file.Flush();
                }
            }

            return templateJarFilePath;
        }

        #endregion
    }
}
