using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExampleAssembly;
using System.CodeDom.Compiler;
using Roslyn.SonarQube.AnalyzerPlugins;
using System.IO;
using Tests.Common;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CSharp;

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
        public void TestSimpleAssemblyLoad()
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
    }
}
