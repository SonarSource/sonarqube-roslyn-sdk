using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginGenerator
{
    public class PluginBuilder
    {
        public const string SONARQUBE_API_VERSION = "4.5.2";

        private readonly IJdkWrapper jdkWrapper;
        private readonly ILogger logger;

        private readonly ISet<string> sourceFiles;
        private readonly ISet<string> referencedJars;

        private readonly IDictionary<string, string> properties;


        private string outputJarFilePath;

        public PluginBuilder(IJdkWrapper jdkWrapper, ILogger logger)
        {
            if (jdkWrapper == null)
            {
                throw new ArgumentNullException("jdkWrapper");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.logger = logger;
            this.jdkWrapper = jdkWrapper;

            this.sourceFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            this.referencedJars = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            this.properties = new Dictionary<string, string>();
        }

        public PluginBuilder(ILogger logger) : this(new JdkWrapper(), logger)
        {
        }

        public PluginBuilder SetJarFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("name");
            }

            this.outputJarFilePath = filePath;

            return this;
        }

        /// <summary>
        /// Sets a plugin property (i.e. a property that will appear in the manifest file)
        /// </summary>
        public PluginBuilder SetProperty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            this.properties[name] = value;
            return this;
        }

        /// <summary>
        /// Adds a source file to be compiled into the plugin
        /// </summary>
        public PluginBuilder AddSourceFile(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentNullException("sourcePath");
            }
            this.sourceFiles.Add(sourcePath);
            return this;
        }

        /// <summary>
        /// Adds a reference to a jar file that is required to compile the source
        /// </summary>
        public PluginBuilder AddReferencedJar(string fullJarFilePath)
        {
            if (string.IsNullOrWhiteSpace(fullJarFilePath))
            {
                throw new ArgumentNullException("fullJarFilePath");
            }
            this.referencedJars.Add(fullJarFilePath);
            return this;
        }

        /// <summary>
        /// Adds a file to the jar. The location of the file in the jar
        /// is specified by the <paramref name="relativeJarPath"/>.
        /// </summary>
        public PluginBuilder AddResourceFile(string relativeJarPath, string fullFilePath)
        {
            if (string.IsNullOrWhiteSpace(fullFilePath))
            {
                throw new ArgumentNullException("fullFilePath");
            }

            return this;
        }

        /// <summary>
        /// Compiles the source files that have been supplied and builds the jar file
        /// </summary>
        public void Build()
        {
            if (!this.jdkWrapper.IsJdkInstalled())
            {
                throw new InvalidOperationException(UIResources.JarB_JDK_NotInstalled);
            }

            // TODO: validate inputs

            // Temp working folder
            string tempWorkingDir = Path.GetTempPath() + Guid.NewGuid().ToString();
            Directory.CreateDirectory(tempWorkingDir);

            // Compile sources
            CompileJavaFiles(tempWorkingDir);

            // Build jar
            BuildJar(tempWorkingDir);
        }



        #region Private methods

        private void CompileJavaFiles(string workingDirectory)
        {
            JavaCompilationBuilder compiler = new JavaCompilationBuilder(this.jdkWrapper);

            // Unpack and reference the required jar files
            SourceGenerator.UnpackReferencedJarFiles(typeof(Generator).Assembly, "PluginGenerator.Resources", workingDirectory);
            foreach (string jarFile in Directory.GetFiles(workingDirectory, "*.jar"))
            {
                compiler.AddClassPath(jarFile);
            }

            foreach (string jarFile in this.referencedJars)
            {
                compiler.AddClassPath(jarFile);
            }

            foreach (string sourceFile in this.sourceFiles)
            {
                compiler.AddSources(sourceFile);
            }

            bool success = compiler.Compile(workingDirectory, workingDirectory, this.logger);

            if (success)
            {
                logger.LogInfo(UIResources.JComp_SourceCompilationSucceeded);
            }
            else
            {
                logger.LogError(UIResources.JComp_SourceCompilationFailed);
                throw new CompilerException(UIResources.JComp_CompliationFailed);
            }
        }

        private bool BuildJar(string classesDirectory)
        {
            JarBuilder jarBuilder = new JarBuilder(logger, this.jdkWrapper);

            // Set the manifest properties
            jarBuilder.SetManifestPropety("Sonar-Version", SONARQUBE_API_VERSION);

            foreach (KeyValuePair<string, string> nameValuePair in this.properties)
            {
                jarBuilder.SetManifestPropety(nameValuePair.Key, nameValuePair.Value);
            }

            // Add the generated classes
            int lenClassPath = classesDirectory.Length + 1;
            foreach (string classFile in Directory.GetFiles(classesDirectory, "*.class", SearchOption.AllDirectories))
            {
                jarBuilder.AddFile(classFile, classFile.Substring(lenClassPath));
            }

            return jarBuilder.Build(this.outputJarFilePath);
        }


        #endregion
    }
}
