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
            public string OutputJar { get; set; }
        }

        public void GeneratePlugin(PluginDefinition definition, string outputDirectory)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentNullException("outputDirectory");
            }

            ValidateDefinition(definition);

            FolderStructure folders = CreateFolderStructure(outputDirectory);

            Dictionary<string, string> replacementMap = new Dictionary<string, string>();

            SourceGenerator.CreateSourceFiles(typeof(PluginGenerator).Assembly, "PluginGenerator/resources/", folders.SourcesRoot, replacementMap);

            CompileJavaFiles(folders);
            
        }

        private static void ValidateDefinition(PluginDefinition definition)
        {

        }

        private static FolderStructure CreateFolderStructure(string outputDirectory)
        {
            FolderStructure folders = new FolderStructure();
            folders.OutputJar = outputDirectory;

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
   
        private static void CompileJavaFiles(FolderStructure folders)
        {
        }

        private static void BuildJar(string jarFileName, string targetDir, string outputDirectory)
        {

        }

    }
}
