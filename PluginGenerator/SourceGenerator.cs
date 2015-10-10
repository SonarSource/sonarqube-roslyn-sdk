using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PluginGenerator
{
    public static class SourceGenerator
    {
        public static void CreateSourceFiles(Assembly resourceAssembly, string rootResourceName, string outputDir, IDictionary<string, string> replacementMap)
        {
            // Unpack the source files into the sources directory
            foreach (string resourceName in resourceAssembly.GetManifestResourceNames().Where(n => n.EndsWith(".java")))
            {
                using (StreamReader reader = new StreamReader(resourceAssembly.GetManifestResourceStream(resourceName)))
                {
                    string content = reader.ReadToEnd();

                    // Substitute in the replacement tags
                    foreach (KeyValuePair<string, string> kvp in replacementMap)
                    {
                        content = content.Replace(kvp.Key, kvp.Value);
                    }

                    string newFilePath = CalculateFilePath(rootResourceName, resourceName, outputDir);
                    File.WriteAllText(newFilePath, content);
                }
            }

        }

        private static string CalculateFilePath(string rootResourceName, string resourceName, string rootOutputPath)
        {
            Debug.Assert(resourceName.IndexOf(rootResourceName) == 0);

            // TODO: create directories
            string relativePath = resourceName.Replace(rootResourceName, string.Empty);

            return Path.Combine(rootOutputPath, relativePath);
        }
    }
}
