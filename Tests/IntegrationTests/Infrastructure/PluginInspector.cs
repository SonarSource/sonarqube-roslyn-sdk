//-----------------------------------------------------------------------
// <copyright file="PluginInspector.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Common;
using SonarQube.Plugins.Common;
using SonarQube.Plugins.Maven;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SonarQube.Plugins.IntegrationTests
{
    /// <summary>
    /// Wrapper around a Java class that loads a plugin from a jar and extracts information
    /// from it. The plugin information is returned as an XML file
    /// </summary>
    internal class PluginInspector
    {
        private const string PluginInspectorFullClassName = "PluginInspector";

        private readonly IJdkWrapper jdkWrapper;
        private readonly ILogger logger;

        private readonly string tempDir;
        private string inspectorClassFilePath;
        
        public PluginInspector(ILogger logger, string tempDir)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (string.IsNullOrWhiteSpace(tempDir))
            {
                throw new ArgumentNullException("tempDir");
            }

            this.logger = logger;
            this.tempDir = tempDir;
            this.jdkWrapper = new JdkWrapper();
            this.CheckJdkIsInstalled();
        }

        public JarInfo GetPluginDescription(string jarFilePath)
        {
            Assert.IsTrue(File.Exists(jarFilePath), "Jar file does not exist");

            this.Build(jarFilePath);

            string reportFilePath = this.RunPluginInspector(jarFilePath, tempDir);

            JarInfo jarInfo = null;
            if (reportFilePath != null)
            {
                jarInfo = JarInfo.Load(reportFilePath);
            }

            return jarInfo;
        }

        private void CheckJdkIsInstalled()
        {
            if (!this.jdkWrapper.IsJdkInstalled())
            {
                Assert.Inconclusive("Test requires the JDK to be installed");
            }
        }

        private void Build(string jarFilePath)
        {
            Assert.IsTrue(File.Exists(jarFilePath), "Jar file does not exist");

            // Get the java source files
            Directory.CreateDirectory(this.tempDir);
            string srcDir = CreateSubDir(this.tempDir, "src");
            string outDir = CreateSubDir(this.tempDir, "out");
            string xxxDir = CreateSubDir(this.tempDir, "xxx");

            SourceGenerator.CreateSourceFiles(this.GetType().Assembly,
                "SonarQube.Plugins.IntegrationTests.Roslyn.Resources",
                srcDir,
                new Dictionary<string, string>());

            JavaCompilationBuilder builder = new JavaCompilationBuilder(this.jdkWrapper);
            foreach (string source in Directory.GetFiles(srcDir, "*.java", SearchOption.AllDirectories))
            {
                builder.AddSources(source);
            }

            // Add the jars required to compile the Java code
            IEnumerable<string> jarFiles = GetCompileDependencies();

            foreach (string jar in jarFiles)
            {
                builder.AddClassPath(jar);
            }

            bool result = builder.Compile(xxxDir, outDir, logger);

            if (!result)
            {
                Assert.Inconclusive("Test setup error: failed to build the Java inspector");
            }

            this.inspectorClassFilePath = GetPluginInspectorClassFilePath(outDir);
        }

        private IEnumerable<string> GetCompileDependencies()
        {
            MavenPartialPOM pom = GetPOMFromResource(typeof(PluginInspector).Assembly, "SonarQube.Plugins.IntegrationTests.Roslyn.Resources.CompileTimeDependencies.pom");
            IEnumerable<string> jarFiles = GetJarsFromPOM(pom);
            return jarFiles;
        }

        private IEnumerable<string> GetRuntimeDependencies()
        {
            MavenPartialPOM pom = GetPOMFromResource(typeof(PluginInspector).Assembly, "SonarQube.Plugins.IntegrationTests.Roslyn.Resources.RuntimeDependencies.pom");
            IEnumerable<string> jarFiles = GetJarsFromPOM(pom);
            return jarFiles;
        }

        private static string CreateSubDir(string rootDir, string subDirName)
        {
            string fullName = Path.Combine(rootDir, subDirName);
            Directory.CreateDirectory(fullName);
            return fullName;
        }

        private static string GetPluginInspectorClassFilePath(string rootDir)
        {
            IEnumerable<string> classFilePaths = Directory.GetFiles(rootDir, "*.class");

            Assert.AreEqual(1, classFilePaths.Count());
            return classFilePaths.First();
        }

        private string RunPluginInspector(string jarFilePath, string outputDIr)
        {
            Debug.Assert(!string.IsNullOrEmpty(this.inspectorClassFilePath));

            string reportFilePath = Path.Combine(outputDIr, "report.xml");

            IEnumerable<string> jarFiles = GetRuntimeDependencies();

            // Construct the class path argument
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("-cp \"{0}\"", Path.GetDirectoryName(this.inspectorClassFilePath));
            foreach (string dependencyFilePath in jarFiles)
            {
                sb.AppendFormat(";{0}", dependencyFilePath);
            }

            IList<string> cmdLineArgs = new List<string>();
            cmdLineArgs.Add(sb.ToString()); // options must preceed the name of the class to execute
            cmdLineArgs.Add(PluginInspectorFullClassName);
            cmdLineArgs.Add(jarFilePath); // parameter(s) to pass to the class
            cmdLineArgs.Add(reportFilePath);

            ProcessRunnerArguments runnerArgs = new ProcessRunnerArguments(GetJavaExePath(), logger);
            runnerArgs.CmdLineArgs = cmdLineArgs;

            ProcessRunner runner = new ProcessRunner();
            bool success = runner.Execute(runnerArgs);

            Assert.IsTrue(success, "Test error: failed to execute the PluginInspector");
            Assert.IsTrue(File.Exists(reportFilePath), "Test error: failed to create the PluginInspector report");
            return reportFilePath;
        }

        private static MavenPartialPOM GetPOMFromResource(Assembly resourceAssembly, string resourceName)
        {
            MavenPartialPOM pom = null;
            using (Stream stream = resourceAssembly.GetManifestResourceStream(resourceName))
            {
                pom = MavenPartialPOM.Load(stream);
            }
            return pom;
        }

        private IEnumerable<string> GetJarsFromPOM(MavenPartialPOM pom)
        {
            Debug.Assert(pom != null);

            IList<string> jarFilePaths = new List<string>();

            MavenArtifactHandler handler = new MavenArtifactHandler(this.logger);

            foreach (MavenDependency dependency in pom.Dependencies)
            {
                string jarFilePath = handler.FetchArtifactJarFile(dependency);
                jarFilePaths.Add(jarFilePath);
            }
            return jarFilePaths;
        }

        private static string GetJavaExePath()
        {
            string javaExeFilePath = Environment.GetEnvironmentVariable("JAVA_HOME");
            Assert.IsFalse(string.IsNullOrWhiteSpace(javaExeFilePath), "Test setup error: cannot locate java.exe because JAVA_HOME is not set");

            javaExeFilePath = Path.Combine(javaExeFilePath, "bin", "java.exe");
            Assert.IsTrue(File.Exists(javaExeFilePath), "Test setup error: failed to locate java.exe - does not exist at '{0}'", javaExeFilePath);
            return javaExeFilePath;
        }
    }
}
