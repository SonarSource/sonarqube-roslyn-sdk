using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PluginGenerator
{
    public class PluginGenerator
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

        public PluginGenerator(IJdkWrapper jdkWrapper)
        {
            if (jdkWrapper == null)
            {
                throw new ArgumentNullException("param");
            }

            this.jdkWrapper = jdkWrapper;
        }

        public void GeneratePlugin(PluginDefinition definition, string outputDirectory, ILogger logger)
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

            SourceGenerator.CreateSourceFiles(typeof(PluginGenerator).Assembly, "PluginGenerator/resources/", folders.SourcesRoot, replacementMap);

            CompileJavaFiles(folders);

        }

        private static void ValidateDefinition(PluginDefinition definition)
        {
            // TODO
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
   
        private void CompileJavaFiles(FolderStructure folders)
        {
            if (!this.jdkWrapper.IsJdkInstalled())
            {
                throw new InvalidOperationException(UIResources.JarB_JDK_NotInstalled);
            }

            
        }

        private static void BuildJar(string jarFileName, string targetDir, string outputDirectory)
        {

        }

    }
}
