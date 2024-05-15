/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2022 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using SonarQube.Plugins.Common;

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
        private const string TemplateJarResourceName = "SonarQube.Plugins.Roslyn.Resources.sonar-roslyn-sdk-template-plugin-1.2.0.76.jar";

        // Locations in the jar archive where various file should be embedded.
        // Using forward-slash since that is the separator used by Java for archive entry names.
        private const string RelativeManifestResourcePath = "META-INF/MANIFEST.MF";
        private const string RelativeConfigurationResourcePath = "org/sonar/plugins/roslynsdk/configuration.xml";
        private const string RelativeRulesXmlResourcePath = "org/sonar/plugins/roslynsdk/rules.xml";

        private readonly ILogger logger;

        private readonly IDictionary<string, string> pluginProperties;
        private readonly JarManifestBuilder jarManifestBuilder;
        private readonly IDictionary<string, string> fileToRelativePathMap;
        private string language;
        private string rulesFilePath;
        private string repositoryKey;
        private string repositoryName;

        #region Public methods

        public RoslynPluginJarBuilder(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            pluginProperties = new Dictionary<string, string>();
            jarManifestBuilder = new JarManifestBuilder();
            fileToRelativePathMap = new Dictionary<string, string>();
        }

        public RoslynPluginJarBuilder SetJarFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            JarFilePath = filePath;

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

            pluginProperties[name] = value;
            return this;
        }

        /// <summary>
        /// Sets a property that will appear in the manifest file
        /// </summary>
        public RoslynPluginJarBuilder SetManifestProperty(string name, string value)
        {
            jarManifestBuilder.SetProperty(name, value);
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
            SetManifestProperty(WellKnownPluginProperties.Key, key);

            return this;
        }

        private void SetNonNullManifestProperty(string property, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                SetManifestProperty(property, value);
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

            fileToRelativePathMap[fullFilePath] = relativeJarPath;

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
            language = ruleLanguage;
            return this;
        }

        public RoslynPluginJarBuilder SetRulesFilePath(string filePath)
        {
            // The existence of the file will be checked before building
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            rulesFilePath = filePath;
            return this;
        }

        public RoslynPluginJarBuilder SetRepositoryKey(string key)
        {
            RepositoryKeyUtilities.ThrowIfInvalid(key);
            repositoryKey = key;
            return this;
        }

        public RoslynPluginJarBuilder SetRepositoryName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            repositoryName = name;
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

            if (File.Exists(JarFilePath))
            {
                logger.LogWarning(UIResources.Builder_ExistingJarWillBeOvewritten);
            }

            BuildJar(tempWorkingDir);
        }

        public string JarFilePath { get; private set; }

        #endregion Public methods

        #region Private methods configuration

        public void BuildJar(string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentNullException(nameof(workingDirectory));
            }

            ValidateConfiguration();

            string templateJarFilePath = ExtractTemplateJarFile(workingDirectory);

            // Create the config and manifest files
            string configFilePath = BuildConfigFile(workingDirectory);
            string manifestFilePath = BuildManifest(templateJarFilePath, workingDirectory);

            // Update the jar
            ArchiveUpdater updater = new ArchiveUpdater(this.logger);

            updater.SetInputArchive(templateJarFilePath)
                .SetOutputArchive(JarFilePath)
                .AddFile(manifestFilePath, RelativeManifestResourcePath)
                .AddFile(configFilePath, RelativeConfigurationResourcePath)
                .AddFile(rulesFilePath, RelativeRulesXmlResourcePath);

            foreach(KeyValuePair<string, string> kvp in fileToRelativePathMap)
            {
                updater.AddFile(kvp.Key, kvp.Value);
            }

            updater.UpdateArchive();
        }

        private void ValidateConfiguration()
        {
            // TODO: validate other inputs
            CheckPropertyIsSet(WellKnownPluginProperties.PluginName);
            string key = CheckPropertyIsSet(WellKnownPluginProperties.Key);
            PluginKeyUtilities.ThrowIfInvalid(key);

            if (string.IsNullOrWhiteSpace(JarFilePath))
            {
                throw new InvalidOperationException(UIResources.Builder_Error_OutputJarPathMustBeSpecified);
            }
        }

        private string CheckPropertyIsSet(string propertyName)
        {
            jarManifestBuilder.TryGetValue(propertyName, out string value);

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

            RoslynSdkConfiguration config = new RoslynSdkConfiguration
            {
                PluginKeyDifferentiator = FindPluginKey(),

                RepositoryKey = repositoryKey,
                RepositoryName = repositoryName,
                RepositoryLanguage = SupportedLanguages.RepositoryLanguage(language),
                RulesXmlResourcePath = GetAbsoluteResourcePath(RelativeRulesXmlResourcePath)
            };

            foreach(KeyValuePair<string,string> kvp in pluginProperties)
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
            jarManifestBuilder.TryGetValue(WellKnownPluginProperties.Key, out string pluginKey);
            if (pluginKey != null)
            {
                pluginKey = PluginKeyUtilities.GetValidKey(pluginKey);
            }
            return pluginKey;
        }

        private static string ExtractTemplateJarFile(string workingDirectory)
        {
            string templateJarFilePath = Path.Combine(workingDirectory, "template.jar");

            using (Stream resourceStream = typeof(RoslynPluginJarBuilder).Assembly.GetManifestResourceStream(TemplateJarResourceName))
            {
                using (FileStream file = new FileStream(templateJarFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    resourceStream.CopyTo(file);
                    file.Flush();
                }
            }

            return templateJarFilePath;
        }

        private string BuildManifest(string templateJarFilePath, string workingDirectory)
        {
            var templateManifest = GetContentsOfFileFromArchive(templateJarFilePath, RelativeManifestResourcePath);

            CopyReservedPropertiesFromExistingManifest(templateManifest);

            var manifestFilePath = jarManifestBuilder.WriteManifest(workingDirectory);
            return manifestFilePath;
        }

        private string GetContentsOfFileFromArchive(string pathToArchive, string fullEntryName)
        {
            string text = null;
            using (var archive = new ZipArchive(new FileStream(pathToArchive, FileMode.Open)))
            {
                var entry = archive.GetEntry(fullEntryName);
                if (entry == null)
                {
                    throw new InvalidOperationException(
                        string.Format(System.Globalization.CultureInfo.CurrentCulture,
                            UIResources.Builder_Error_EntryNotFoundInTemplatePlugin, fullEntryName));
                }

                var buffer = new byte[entry.Length];
                using (var entryStream = entry.Open())
                {
                    entryStream.Read(buffer, 0, buffer.Length);
                    text = System.Text.Encoding.UTF8.GetString(buffer);
                }
            }

            return text;
        }

        /// <summary>
        /// Some of the properties in the template manifest should
        /// be preserved e.g. the supported SonarQube version
        /// </summary>
        private void CopyReservedPropertiesFromExistingManifest(string templateManifest)
        {
            var reader = new JarManifestReader(templateManifest);
            CopyValueFromExistingManifest(reader, "Sonar-Version");
            CopyValueFromExistingManifest(reader, "Plugin-Dependencies");
            CopyValueFromExistingManifest(reader, "Plugin-Class");
            CopyValueFromExistingManifest(reader, "SonarLint-Supported"); // applies to other IDEs i.e. not VS
        }

        private void CopyValueFromExistingManifest(JarManifestReader reader, string property)
        {
            if (jarManifestBuilder.TryGetValue(property, out string _))
            {
                throw new InvalidOperationException(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        UIResources.Builder_Error_ManifestPropertyShouldBeCopiedFromTemplate, property));
            }
            jarManifestBuilder.SetProperty(property, reader.GetValue(property));
        }

        #endregion Private methods configuration
    }
}