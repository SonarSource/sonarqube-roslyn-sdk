using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PluginGenerator
{
    public static class SourceGenerator
    {
        /// <summary>
        /// Creates source files on disk from resources embedded in the supplied assembly
        /// </summary>
        /// <param name="resourceAssembly">Assembly containing the resources</param>
        /// <param name="rootResourceName">Root name for the resources that should be extracted</param>
        /// <param name="outputDir">The directory to which the source files should be extracted</param>
        /// <param name="replacementMap">List of placeholder and replacement values to substitute into the resources</param>
        /// <remarks>Only .java files will be extracted. The directory structure will be created on disk based
        /// on the separators in the resource name e.g. a resource called myorg.myapp.class1.java will be
        /// extracted into myorg\myapp\class1.java</remarks>
        public static void CreateSourceFiles(Assembly resourceAssembly, string rootResourceName, string outputDir, IDictionary<string, string> replacementMap)
        {
            string fixedRootResourceName = rootResourceName;
            if (!fixedRootResourceName.EndsWith("."))
            {
                fixedRootResourceName += ".";
            }

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

                    string newFilePath = CalculateSourceFilePath(fixedRootResourceName, resourceName, outputDir);

                    if (newFilePath != null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                        File.WriteAllText(newFilePath, content);
                    }
                }
            }

        }

        private static string CalculateSourceFilePath(string rootResourceName, string resourceName, string rootOutputPath)
        {
            if (!resourceName.StartsWith(rootResourceName))
            {
                return null;
            }

            string relativePath = resourceName.Replace(rootResourceName, string.Empty);
            relativePath = relativePath.Trim('.');

            string[] fragments = relativePath.Split('.');
            Debug.Assert(fragments.Length > 1, "Expecting at least two parts to the file name and path");

            string dir = null;
            string fileName = null;
            if (fragments.Length > 2)
            {
                dir = string.Join("\\", fragments.Take(fragments.Length - 2));
                fileName = string.Join(".", fragments.Skip(fragments.Length - 2));
            }
            else
            {
                fileName = relativePath;
            }

            return Path.Combine(rootOutputPath, dir ?? string.Empty, fileName);
        }

        public static void UnpackReferencedJarFiles(Assembly resourceAssembly, string rootResourceName, string outputDir)
        {
            // Unpack the embedded jar files into the output directory
            foreach (string resourceName in resourceAssembly.GetManifestResourceNames().Where(n => n.EndsWith(".jar")))
            {
                string fullJarPath = Path.Combine(outputDir, CalculateJarFileName(rootResourceName, resourceName));

                byte[] buffer = new byte[1024];
                using (BinaryReader reader = new BinaryReader(resourceAssembly.GetManifestResourceStream(resourceName)))
                using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(fullJarPath)))
                {
                    int bytesRead;                    
                    do
                    {
                        bytesRead = reader.Read(buffer, 0, buffer.Length);
                        writer.Write(buffer, 0, bytesRead);
                    } while (bytesRead > 0) ;
                }
            }

        }

        private static string CalculateJarFileName(string rootResourceName, string fullResourceName)
        {
            if (!fullResourceName.StartsWith(rootResourceName))
            {
                return null;
            }

            Debug.Assert(fullResourceName.EndsWith(".jar"), "Expecting the resource to be a jar file");

            string relativePath = fullResourceName.Replace(rootResourceName, string.Empty);
            relativePath = relativePath.Trim('.');

            return relativePath;
        }
    }
}
