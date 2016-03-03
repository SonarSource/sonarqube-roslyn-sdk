//-----------------------------------------------------------------------
// <copyright file="PluginBuilder.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Plugins.Common;
using SonarQube.Plugins.Maven;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SonarQube.Plugins
{
    /// <summary>
    /// Creates a SonarQube plugin
    /// </summary>
    public class PluginBuilder
    {
        public const string SONARQUBE_API_VERSION = "4.5.2";

        /// <summary>
        /// The name of the package in which the plugin will be created
        /// </summary>
        private const string CorePluginPackageNameTemplate = "org.sonarqube.plugin.sdk.[PLUGIN_KEY]";

        /// <summary>
        /// The name of the class that SonarQube should load to discover the
        /// extensions offered by the plugin
        /// </summary>
        private const string CorePluginEntryClassTemplate = "[PLUGIN_PACKAGE].Plugin";

        /// <summary>
        /// Token that should be replaced with a list of the classes that exposed by
        /// the plugin as a extensions.
        /// Sample value: "org.sonarqube.plugin.sdk.MyRulesDefinition.class, myorg.otherExtension.class"
        /// </summary>
        private const string ExtensionListToken = "[CORE_EXTENSION_CLASS_LIST]";


        private const string CoreResourcesRoot = "SonarQube.Plugins.Resources.Core.";
        private const string CorePOMResourceName = CoreResourcesRoot + "Core.pom";

        private readonly IJdkWrapper jdkWrapper;
        private readonly IMavenArtifactHandler artifactHandler;
        private readonly ILogger logger;
        private readonly ISet<string> sourceFiles;
        private readonly ISet<string> referencedJars;
        private readonly ISet<string> extensionClasses;

        private readonly IDictionary<string, string> properties;
        private readonly IDictionary<string, string> fileToRelativePathMap;
        private readonly IDictionary<string, string> sourceFileReplacements;

        private string outputJarFilePath;

        /// <summary>
        /// List of jar files that are available at runtime and so do not
        /// need to be embedded in the jar file
        /// </summary>
        private static readonly string[] availableJarFiles = new string[]
        {
            "sonar-plugin-api-4.5.2.jar",
            "slf4j-api-1.7.5.jar" // available in the sonar-plugin-api
        };

        #region Public methods

        public PluginBuilder(IJdkWrapper jdkWrapper, IMavenArtifactHandler artifactHandler, ILogger logger)
        {
            if (jdkWrapper == null)
            {
                throw new ArgumentNullException(nameof(jdkWrapper));
            }
            if (artifactHandler == null)
            {
                throw new ArgumentNullException(nameof(artifactHandler));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.jdkWrapper = jdkWrapper;
            this.artifactHandler = artifactHandler;
            this.logger = logger;

            this.sourceFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            this.referencedJars = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            this.properties = new Dictionary<string, string>();
            this.fileToRelativePathMap = new Dictionary<string, string>();
            this.sourceFileReplacements = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            this.extensionClasses = new HashSet<string>(StringComparer.InvariantCulture); // class names are case-sensitive
        }

        public PluginBuilder(ILogger logger) : this(new JdkWrapper(), new MavenArtifactHandler(logger), logger)
        {
        }

        public PluginBuilder SetJarFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
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
                throw new ArgumentNullException(nameof(name));
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
                throw new ArgumentNullException(nameof(sourcePath));
            }
            this.sourceFiles.Add(sourcePath);
            return this;
        }

        /// <summary>
        /// Specifies a value to replace a source code token with before compiling
        /// </summary>
        public PluginBuilder SetSourceCodeTokenReplacement(string token, string value)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            this.sourceFileReplacements[token] = value;
            return this;
        }

        /// <summary>
        /// Adds a reference to a jar file that is required to compile the source
        /// </summary>
        public PluginBuilder AddReferencedJar(string fullJarFilePath)
        {
            if (string.IsNullOrWhiteSpace(fullJarFilePath))
            {
                throw new ArgumentNullException(nameof(fullJarFilePath));
            }
            this.referencedJars.Add(fullJarFilePath);
            return this;
        }

        /// <summary>
        /// Registers an extension that will be exported to SonarQube
        /// </summary>
        /// <param name="extension"></param>
        /// <remarks>The extension could be an instance of a class (such as a PropertyDefinition), or it could
        /// be a type that SonarQube should instantiate (e.g. a subclass of RulesDefinition). If the extension is
        /// a type then it should end with ".class".</remarks>
        public PluginBuilder AddExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentNullException(nameof(extension));
            }

            this.extensionClasses.Add(extension);
            return this;
        }

        /// <summary>
        /// Adds a file to the jar. The location of the file in the jar
        /// is specified by the <paramref name="relativeJarPath"/>.
        /// </summary>
        public PluginBuilder AddResourceFile(string fullFilePath, string relativeJarPath)
        {
            if (string.IsNullOrWhiteSpace(fullFilePath))
            {
                throw new ArgumentNullException(nameof(fullFilePath));
            }

            this.fileToRelativePathMap[fullFilePath] = relativeJarPath;

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

            // Temp working folder
            string tempWorkingDir = Utilities.CreateTempDirectory(".builder");
            tempWorkingDir = Utilities.CreateSubDirectory(tempWorkingDir, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempWorkingDir);

            this.ApplyConfiguration(tempWorkingDir); // sub-classes will apply their configuration here

            this.ReplaceTokensInSources();

            this.CompileJavaFiles(tempWorkingDir);

            if (File.Exists(this.JarFilePath))
            {
                this.logger.LogWarning(UIResources.CoreBuilder_ExistingJarWillBeOvewritten);
            }

            this.BuildJar(tempWorkingDir);
        }

        public string JarFilePath { get { return this.outputJarFilePath; } }

        #endregion

        #region Virtual methods

        /// <summary>
        /// Sub-classes should override this method to perform any necessary validation
        /// and configuration e.g. adding specific jar files
        /// </summary>
        /// <param name="baseWorkingDirectory">Shared base working directory for temporary files</param>
        /// <remarks>Sub-classes should perform their configuration (e.g. adding extension classes) before calling base.ApplyConfiguration(...)</remarks>
        protected virtual void ApplyConfiguration(string baseWorkingDirectory)
        {
            this.ValidateCoreConfiguration();

            string tempDir = Utilities.CreateSubDirectory(baseWorkingDirectory, ".core");

            AddCoreJars();
            AddCoreSources(tempDir);
            ConfigureCoreSourceFileReplacements();
        }

        #endregion

        #region Protected methods

        protected ILogger Logger { get { return this.logger; } }

        protected IMavenArtifactHandler ArtifactHandler { get { return this.artifactHandler; }  }

        protected void SetSourceFileReplacement(string key, string value)
        {
            this.sourceFileReplacements[key] = value;
        }

        protected string FindPluginKey()
        {
            string pluginKey;
            this.properties.TryGetValue(WellKnownPluginProperties.Key, out pluginKey);
            if (pluginKey != null)
            {
                pluginKey = PluginKeyUtilities.GetValidKey(pluginKey);
            }
            return pluginKey;
        }

        protected string FindPackageName()
        {
            string packageName = null;

            string pluginKey = this.FindPluginKey();
            if (pluginKey != null)
            {
                packageName = CorePluginPackageNameTemplate.Replace(WellKnownSourceCodeTokens.Core_PluginKey, pluginKey);
            }
            return packageName;
        }

        #endregion

        #region Core builder configuration

        private void ValidateCoreConfiguration()
        {
            // TODO: validate other inputs
            this.CheckPropertyIsSet(WellKnownPluginProperties.PluginName);
            string key = this.CheckPropertyIsSet(WellKnownPluginProperties.Key);
            PluginKeyUtilities.ThrowIfInvalid(key);

            if (this.extensionClasses == null || !this.extensionClasses.Any())
            {
                throw new InvalidOperationException(UIResources.CoreBuilder_MustSpecifyAnExtensionClass);
            }

            if (string.IsNullOrWhiteSpace(this.JarFilePath))
            {
                throw new InvalidOperationException(UIResources.CoreBuilder_Error_OutputJarPathMustBeSpecified);
            }
        }

        private string CheckPropertyIsSet(string propertyName)
        {
            string value;
            this.properties.TryGetValue(propertyName, out value);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    UIResources.CoreBuilder_Error_RequiredPropertyMissing, propertyName));
            }
            return value;
        }

        private string FindPluginName()
        {
            string pluginName;
            this.properties.TryGetValue(WellKnownPluginProperties.PluginName, out pluginName);
            return pluginName;
        }

        private void AddCoreSources(string workingDirectory)
        {
            SourceGenerator.CreateSourceFiles(typeof(RulesPluginBuilder).Assembly, CoreResourcesRoot, workingDirectory, new Dictionary<string, string>());

            foreach (string sourceFile in Directory.GetFiles(workingDirectory, "*.java", SearchOption.AllDirectories))
            {
                this.AddSourceFile(sourceFile);
            }
        }

        private void ConfigureCoreSourceFileReplacements()
        {
            string pluginKey = this.FindPluginKey();
            string packageName = this.FindPackageName();
            string fullPluginClassName = CorePluginEntryClassTemplate.Replace(WellKnownSourceCodeTokens.Core_PluginPackage, packageName);

            Debug.Assert(!string.IsNullOrWhiteSpace(pluginKey), "Plugin key should be set");
            Debug.Assert(!string.IsNullOrWhiteSpace(packageName), "Package name should be set");

            this.SetSourceFileReplacement(WellKnownSourceCodeTokens.Core_PluginPackage, packageName);
            this.SetSourceFileReplacement(WellKnownSourceCodeTokens.Core_PluginKey, pluginKey);
            this.SetSourceFileReplacement(WellKnownSourceCodeTokens.Core_PluginName, this.FindPluginName());

            this.SetProperty(WellKnownPluginProperties.Class, fullPluginClassName);

            // Build and set the list of extensions to be exported
            string javaList = string.Join(", " + Environment.NewLine, this.extensionClasses);
            this.SetSourceFileReplacement(ExtensionListToken, javaList);
        }

        private void AddCoreJars()
        {
            // Fetch and reference the required jar files
            MavenPartialPOM pom = this.ArtifactHandler.GetPOMFromResource(this.GetType().Assembly, CorePOMResourceName);
            IEnumerable<string> jarFiles = this.ArtifactHandler.GetJarsFromPOM(pom);

            foreach (string jarFile in jarFiles)
            {
                this.AddReferencedJar(jarFile);
            }
        }

        #endregion

        #region Compile and package implementation

        /// <summary>
        /// Perform token-based substitution into the supplied source files prior to compiling
        /// </summary>
        private void ReplaceTokensInSources()
        {
            foreach (string sourceFile in this.sourceFiles)
            {
                string content = File.ReadAllText(sourceFile);

                // Substitute values in source files
                foreach (KeyValuePair<string, string> kvp in this.sourceFileReplacements)
                {
                    content = content.Replace(kvp.Key, kvp.Value);
                }

                File.WriteAllText(sourceFile, content);
            }
        }

        private void CompileJavaFiles(string workingDirectory)
        {
            JavaCompilationBuilder compiler = new JavaCompilationBuilder(this.jdkWrapper);

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
                throw new JavaCompilerException(UIResources.JComp_CompliationFailed);
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

            // Add any other content files
            foreach(KeyValuePair<string, string> pathToFilePair in this.fileToRelativePathMap)
            {
                jarBuilder.AddFile(pathToFilePair.Key, pathToFilePair.Value);
            }

            // Embed all referenced jars into the jar
            // NB not all jars need to be added
            StringBuilder sb = new StringBuilder();
            foreach (string refJar in this.referencedJars)
            {
                if (IsJarAvailable(refJar))
                {
                    continue;
                }

                string jarName = "META-INF/lib/" + Path.GetFileName(refJar);
                jarBuilder.AddFile(refJar, jarName);

                sb.Append(jarName);
                sb.Append(" ");
            }
            jarBuilder.SetManifestPropety("Plugin-Dependencies", sb.ToString());

            return jarBuilder.Build(this.outputJarFilePath);
        }

        private static bool IsJarAvailable(string resourceName)
        {
            return availableJarFiles.Any(j => resourceName.EndsWith(j));
        }

        #endregion
    }
}
