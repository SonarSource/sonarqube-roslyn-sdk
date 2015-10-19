using System;
using System.Collections.Generic;
using System.IO;

namespace PluginGenerator
{
    public class Generator
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

        public Generator(IJdkWrapper jdkWrapper)
        {
            if (jdkWrapper == null)
            {
                throw new ArgumentNullException("param");
            }
            
            this.jdkWrapper = jdkWrapper;
        }

        public bool GeneratePlugin(PluginDefinition definition, string outputDirectory, ILogger logger)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentNullException("outputDirectory");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            ValidateDefinition(definition);

            FolderStructure folders = CreateFolderStructure(outputDirectory);

            Dictionary<string, string> replacementMap = new Dictionary<string, string>();
            PopulateSourceFileReplacements(definition, replacementMap);

            SourceGenerator.CreateSourceFiles(typeof(Generator).Assembly, "PluginGenerator.Resources.", folders.SourcesRoot, replacementMap);

            bool success = CompileJavaFiles(folders, logger);

            if (success)
            {
                string jarFullPath = Path.Combine(outputDirectory, definition.Key + ".jar");
                success = BuildJar(definition, jarFullPath, folders.CompiledClasses, logger);
            }

            return success;
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

        private static FolderStructure CreateFolderStructure(string outputDirectory)
        {
            FolderStructure folders = new FolderStructure();
            folders.OutputJarFilePath = outputDirectory;

            folders.WorkingDir = Path.GetTempPath() + Guid.NewGuid().ToString();

            folders.SourcesRoot = Path.Combine(folders.WorkingDir, "src");
            folders.Resources = Path.Combine(folders.WorkingDir, "resources");
            folders.CompiledClasses = Path.Combine(folders.WorkingDir, "classes");
            folders.References= Path.Combine(folders.WorkingDir, "references");

            Directory.CreateDirectory(folders.WorkingDir);
            Directory.CreateDirectory(folders.SourcesRoot);
            Directory.CreateDirectory(folders.CompiledClasses);
            Directory.CreateDirectory(folders.Resources);
            Directory.CreateDirectory(folders.References);

            return folders;
        }

        private static void PopulateSourceFileReplacements(PluginDefinition definition, IDictionary<string, string> replacementMap)
        {
            replacementMap.Add("[LANGUAGE]", definition.Language);
            replacementMap.Add("[PLUGIN_KEY]", definition.Key);
            replacementMap.Add("[PLUGIN_NAME]", definition.Name);
        }

        private bool CompileJavaFiles(FolderStructure folders, ILogger logger)
        {
            if (!this.jdkWrapper.IsJdkInstalled())
            {
                throw new InvalidOperationException(UIResources.JarB_JDK_NotInstalled);
            }

            JavaCompilationBuilder builder = new JavaCompilationBuilder(this.jdkWrapper);

            // Unpack and reference the required jar files
            SourceGenerator.UnpackReferencedJarFiles(typeof(Generator).Assembly, "PluginGenerator.Resources", folders.References);
            foreach (string jarFile in Directory.GetFiles(folders.References, "*.jar"))
            {
                builder.AddClassPath(jarFile);
            }

            // Add the source files
            foreach(string sourceFile in Directory.GetFiles(folders.SourcesRoot, "*.java", SearchOption.AllDirectories))
            {
                builder.AddSources(sourceFile);
            }

            bool success = builder.Compile(folders.SourcesRoot, folders.CompiledClasses, logger);

            if (success)
            {
                logger.LogInfo(UIResources.JComp_SourceCompilationSucceeded);
            }
            else
            {
                logger.LogError(UIResources.JComp_SourceCompilationFailed);
            }
            return success;
        }

        private bool BuildJar(PluginDefinition defn, string fullJarFilePath, string classesDirectory, ILogger logger)
        {
            JarBuilder builder = new JarBuilder(logger, this.jdkWrapper);

            AddPluginManifestProperties(defn, builder);

            int lenClassPath = classesDirectory.Length + 1;
            foreach (string classFile in Directory.GetFiles(classesDirectory, "*.class", SearchOption.AllDirectories))
            {
                builder.AddFile(classFile, classFile.Substring(lenClassPath));
            }

            return builder.Build(fullJarFilePath);
        }

        private static void AddPluginManifestProperties(PluginDefinition defn, JarBuilder builder)
        {
            //TODO
        }


    }
}
