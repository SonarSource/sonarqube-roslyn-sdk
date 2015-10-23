using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginGenerator
{
    public class Generator2
    {
        private class FolderStructure
        {
            public string WorkingDir { get; set; }
            public string SourcesRoot { get; set; }
            public string Resources { get; set; }
            public string CompiledClasses { get; set; }
            public string References { get; set; }
            public string OutputJarFilePath { get; set; }
        }

        private readonly IJdkWrapper jdkWrapper;
        private readonly ILogger logger;

        public Generator2(IJdkWrapper jdkWrapper, ILogger logger)
        {
            if (jdkWrapper == null)
            {
                throw new ArgumentNullException("param");
            }

            this.jdkWrapper = jdkWrapper;
            this.logger = logger;
        }

        public void GeneratePlugin(PluginDefinition definition, string fullJarFilePath)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            if (string.IsNullOrWhiteSpace(fullJarFilePath))
            {
                throw new ArgumentNullException("fullJarFilePath");
            }
            if (File.Exists(fullJarFilePath))
            {
                throw new ArgumentException(UIResources.Gen_Error_JarFileExists, fullJarFilePath);
            }

            ValidateDefinition(definition);

            // Temp folder which resources will be unpacked into
            string tempWorkingDir = Path.GetTempPath() + Guid.NewGuid().ToString();
            Directory.CreateDirectory(tempWorkingDir);

            BuildPlugin(definition, tempWorkingDir);
        }

        private static void ValidateDefinition(PluginDefinition definition)
        {
            // TODO
            CheckPropertyIsSet(definition.Language, "Language");

            CheckPropertyIsSet(definition.Key, "Key");
            CheckPropertyIsSet(definition.Name, "Name");
        }

        private static void CheckPropertyIsSet(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    UIResources.Error_MissingProperty, name));
            }
        }

        private void BuildPlugin(PluginDefinition definition, string workingFolder)
        {
            if (!this.jdkWrapper.IsJdkInstalled())
            {
                throw new InvalidOperationException(UIResources.JarB_JDK_NotInstalled);
            }

            PluginBuilder builder = new PluginBuilder(this.jdkWrapper, this.logger);

            //// Unpack and reference the required jar files
            //SourceGenerator.UnpackReferencedJarFiles(typeof(Generator).Assembly, "PluginGenerator.Resources", workingFolder);
            //foreach (string jarFile in Directory.GetFiles(workingFolder, "*.jar"))
            //{
            //    builder.AddReferencedJar(jarFile);
            //}

            // No additional jar files apart from the SonarQube API jar are required for this source

            // Generate the source files
            Dictionary<string, string> replacementMap = new Dictionary<string, string>();
            PopulateSourceFileReplacements(definition, replacementMap);
            SourceGenerator.CreateSourceFiles(typeof(Generator).Assembly, "PluginGenerator.Resources.", workingFolder, replacementMap);

            // Add the source files
            foreach (string sourceFile in Directory.GetFiles(workingFolder, "*.java", SearchOption.AllDirectories))
            {
                builder.AddSourceFile(sourceFile);
            }

            AddPluginManifestProperties(definition, builder);

            builder.Build();
        }

        private static void PopulateSourceFileReplacements(PluginDefinition definition, IDictionary<string, string> replacementMap)
        {
            replacementMap.Add("[LANGUAGE]", definition.Language);
            replacementMap.Add("[PLUGIN_KEY]", definition.Key);
            replacementMap.Add("[PLUGIN_NAME]", definition.Name);
        }

        private static void AddPluginManifestProperties(PluginDefinition defn, PluginBuilder builder)
        {
            builder.SetProperty("Plugin-Class", "myorg." + defn.Key + ".Plugin");
            //            SetNonNullManifestProperty("Plugin-Class", defn.Class, builder);

            SetNonNullManifestProperty("Plugin-License", defn.License, builder);
            SetNonNullManifestProperty("Plugin-OrganizationUrl", defn.OrganizationUrl, builder);
            SetNonNullManifestProperty("Plugin-Version", defn.Version, builder);
            SetNonNullManifestProperty("Plugin-Homepage", defn.Homepage, builder);
            SetNonNullManifestProperty("Plugin-SourcesUrl", defn.SourcesUrl, builder);
            SetNonNullManifestProperty("Plugin-Developers", defn.Developers, builder);
            SetNonNullManifestProperty("Plugin-IssueTrackerUrl", defn.IssueTrackerUrl, builder);
            SetNonNullManifestProperty("Plugin-TermsConditionsUrl", defn.TermsConditionsUrl, builder);
            SetNonNullManifestProperty("Plugin-Organization", defn.Organization, builder);
            SetNonNullManifestProperty("Plugin-Name", defn.Name, builder);
            SetNonNullManifestProperty("Plugin-Description", defn.Description, builder);
            SetNonNullManifestProperty("Plugin-Key", defn.Key, builder);
        }

        private static void SetNonNullManifestProperty(string property, string value, PluginBuilder pluginBuilder)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                pluginBuilder.SetProperty(property, value);
            }
        }



    }
}
