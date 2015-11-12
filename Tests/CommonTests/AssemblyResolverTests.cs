using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube.AnalyzerPlugins;
using System.IO;
using Tests.Common;
using System.Reflection;
using Roslyn.SonarQube.Common;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace CommonTests
{
    [TestClass]
    public class AssemblyResolverTests
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Compiles the supplied code into a new assembly
        /// </summary>
        private static Assembly CompileAssembly(string code, string outputFilePath, ILogger logger)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();

            CompilerParameters options = new CompilerParameters();
            options.OutputAssembly = outputFilePath;
            options.GenerateExecutable = true;
            options.GenerateInMemory = false;

            CompilerResults result = provider.CompileAssemblyFromSource(options, code);

            if (result.Errors.Count > 0)
            {
                foreach (string item in result.Output)
                {
                    logger.LogInfo(item);
                }
                Assert.Fail("Test setup error: failed to create dynamic assembly. See the test output for compiler output");
            }

            return result.CompiledAssembly;
        }

        private static Assembly CompileSimpleAssembly(string outputFilePath, ILogger logger)
        {
            return CompileAssembly(@"public class SimpleProgram {
              public static void Main(string[] args) {
                System.Console.WriteLine(""Hello World"");
              }
            }", outputFilePath, logger);
        }

        #region tests

        /// <summary>
        /// Tests that method for creating file names from assembly names is correct.
        /// </summary>
        [TestMethod]
        public void TestAssemblyNameFileNameAssociation()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            Assembly assembly = typeof(AssemblyResolver).Assembly;
            string assemblyName = assembly.FullName;
            string actualFileName = Path.GetFileName(assembly.Location);

            // Act
            string testFileName = AssemblyResolver.CreateFileNameFromAssemblyName(assemblyName);

            // Assert
            Assert.AreEqual<string>(actualFileName, testFileName);
        }

        /// <summary>
        /// Tests the loading of an assembly with a single type and no dependencies. This should succeed even without AssemblyResolver.
        /// </summary>
        [TestMethod]
        public void TestNoImpactOnDefaultResolution()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);
            string simpleAssemblyPath = Path.Combine(testFolder, "SimpleAssembly.dll");
            Assembly simpleAssembly = CompileSimpleAssembly(simpleAssemblyPath, logger);

            object simpleObject = null;

            // Act
            using (new AssemblyResolver(logger, testFolder))
            {
                // Look in every assembly under the supplied directory
                foreach (string assemblyPath in Directory.GetFiles(testFolder, "*.dll", SearchOption.AllDirectories))
                {
                    Assembly assembly = Assembly.LoadFile(assemblyPath);
                    
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (!type.IsAbstract)
                        {
                            simpleObject = Activator.CreateInstance(type);
                        }
                    }
                }
            }

            // Assert
            Assert.IsNotNull(simpleObject);
            Assert.AreEqual<string>("SimpleProgram", simpleObject.GetType().ToString());
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
            using (AssemblyResolver assemblyResolver = new AssemblyResolver(logger, testFolder))
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
            String simpleAssemblyPath = Path.Combine(testFolder, "SimpleAssembly.dll");
            Assembly simpleAssembly = CompileSimpleAssembly(simpleAssemblyPath, logger);
            
            // Act
            Assembly resolveResult;
            using (AssemblyResolver assemblyResolver = new AssemblyResolver(logger, testFolder))
            {
                ResolveEventArgs resolveEventArgs = new ResolveEventArgs(simpleAssembly.FullName, this.GetType().Assembly);
                resolveResult = assemblyResolver.CurrentDomain_AssemblyResolve(this, resolveEventArgs);
            }

            // Assert
            Assert.IsNotNull(resolveResult);
            Assert.AreEqual<string>(simpleAssembly.ToString(), resolveResult.ToString());
            Assert.AreEqual<string>(simpleAssemblyPath, resolveResult.Location);
        }

        #endregion
    }
}
