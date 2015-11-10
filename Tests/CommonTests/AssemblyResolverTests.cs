using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExampleAssembly;
using Roslyn.SonarQube.AnalyzerPlugins;
using System.IO;
using Tests.Common;
using System.Collections.Generic;
using System.Reflection;

namespace CommonTests
{
    [TestClass]
    public class AssemblyResolverTests
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Tests the loading of an assembly with a single type and no dependencies. This should succeed even without AssemblyResolver.
        /// </summary>
        [TestMethod]
        public void TestGeneralAssemblyLoad()
        {
            // Arrange
            TestLogger logger = new TestLogger();

            string simpleAssemblyPath = typeof(SimpleProgram).Assembly.Location;
            string simpleAssemblyFolder = Path.GetDirectoryName(simpleAssemblyPath);
            SimpleProgram simpleProgram = null;
            Type simpleProgramType = null;

            // Act
            using (new AssemblyResolver(simpleAssemblyFolder, logger))
            {
                // Look in every assembly under the supplied directory to see if
                // we can find and create any analyzers
                foreach (string assemblyPath in Directory.GetFiles(simpleAssemblyFolder, "*.dll", SearchOption.AllDirectories))
                {
                    Assembly assembly = Assembly.LoadFile(assemblyPath);
                    
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (!type.IsAbstract && type == typeof(SimpleProgram))
                        {
                            simpleProgram = (SimpleProgram)Activator.CreateInstance(type);
                            simpleProgramType = type;
                        }
                    }
                }
            }

            // Assert
            Assert.IsNotNull(simpleProgram);
            Assert.AreEqual<Type>(simpleProgramType, typeof(SimpleProgram));
        }

        /// <summary>
        /// Tests the case where assembly resolution should fail correctly.
        /// </summary>
        [TestMethod]
        public void TestAssemblyResolutionFail()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);

            // Act
            Assembly resolveResult;
            using (AssemblyResolver assemblyResolver = new AssemblyResolver(testFolder, logger))
            {
                ResolveEventArgs resolveEventArgs = new ResolveEventArgs("nonexistent library", this.GetType().Assembly);
                resolveResult = assemblyResolver.CurrentDomain_AssemblyResolve(this, resolveEventArgs);
            }

            // Assert
            Assert.IsNull(resolveResult);
        }

        /// <summary>
        /// Tests the case where assembly resolution should succeed.
        /// </summary>
        [TestMethod]
        public void TestAssemblyResolution()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);

            Assembly sourceAssembly = typeof(SimpleProgram).Assembly;
            string sourceAssemblyPath = sourceAssembly.Location;
            string destinationAssemblyPath = Path.Combine(testFolder, Path.GetFileName(sourceAssemblyPath));
            File.Copy(sourceAssemblyPath, destinationAssemblyPath);

            // Act
            Assembly resolveResult;
            using (AssemblyResolver assemblyResolver = new AssemblyResolver(testFolder, logger))
            {
                ResolveEventArgs resolveEventArgs = new ResolveEventArgs(sourceAssembly.FullName, this.GetType().Assembly);
                resolveResult = assemblyResolver.CurrentDomain_AssemblyResolve(this, resolveEventArgs);
            }

            // Assert
            Assert.IsNotNull(resolveResult);
            Assert.AreEqual<string>(sourceAssembly.ToString(), resolveResult.ToString());
        }
    }
}
