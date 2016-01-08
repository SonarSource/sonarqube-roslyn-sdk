//-----------------------------------------------------------------------
// <copyright file="AssemblyResolverTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using SonarQube.Plugins.Test.Common;
using System.Reflection;
using SonarQube.Plugins.Common;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace SonarQube.Plugins.CommonTests
{
    [TestClass]
    public class AssemblyResolverTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        /// <summary>
        /// Tests the loading of an assembly with a single type and no dependencies. This should succeed even without AssemblyResolver.
        /// </summary>
        [TestMethod]
        public void AssemblyResolver_NoImpactOnDefaultResolution()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);
            CompileSimpleAssembly("SimpleAssembly.dll", testFolder, logger);

            object simpleObject = null;

            // Act
            using (AssemblyResolver resolver = new AssemblyResolver(logger, testFolder))
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

                // Assert
                Assert.IsNotNull(simpleObject);
                Assert.AreEqual<string>("SimpleProgram", simpleObject.GetType().ToString());
                AssertResolverCaller(resolver);

            }
        }

        /// <summary>
        /// Tests the case where assembly resolution should fail correctly.
        /// </summary>
        [TestMethod]
        public void AssemblyResolver_NonExistentAssembly_ResolutionFails()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);

            // Act
            using (AssemblyResolver resolver = new AssemblyResolver(logger, testFolder))
            {
                AssertAssemblyLoadFails("nonexistent library");

                // Assert
                AssertResolverCaller(resolver);
            }
        }

        /// <summary>
        /// Tests the case where assembly resolution should succeed.
        /// </summary>
        [TestMethod]
        public void AssemblyResolver_ResolutionByFullAssemblyName_Succeeds()
        {
            // Arrange
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);
            Assembly testAssembly = CompileSimpleAssembly("SimpleAssemblyByFullName.dll", testFolder, new TestLogger());

            // Act
            Assembly resolvedAssembly = AssertAssemblyLoadSucceedsOnlyWithResolver("SimpleAssemblyByFullName, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null", testFolder);

            // Assert
            AssertExpectedAssemblyLoaded(testAssembly, resolvedAssembly);
        }

        /// <summary>
        /// Tests the case where assembly resolution should succeed.
        /// </summary>
        [TestMethod]
        public void AssemblyResolver_ResolutionByFileName_Succeeds()
        {
            // Arrante
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);
            Assembly testAssembly = CompileSimpleAssembly("SimpleAssemblyByFileName.dll", testFolder, new TestLogger());

            // Act
            Assembly resolvedAssembly = AssertAssemblyLoadSucceedsOnlyWithResolver("SimpleAssemblyByFileName.dll", testFolder);

            // Assert
            AssertExpectedAssemblyLoaded(testAssembly, resolvedAssembly);
        }

        /// <summary>
        /// Tests the case where assembly resolution should succeed.
        /// </summary>
        [TestMethod]
        public void AssemblyResolver_ResolutionByFullAssemblyNameWithSpace_Succeeds()
        {
            // Arrange
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);
            Assembly testAssembly = CompileSimpleAssembly("Space in Name ByFullName.dll", testFolder, new TestLogger());

            // Act
            Assembly resolvedAssembly = AssertAssemblyLoadSucceedsOnlyWithResolver("Space in Name ByFullName, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null", testFolder);

            // Assert
            AssertExpectedAssemblyLoaded(testAssembly, resolvedAssembly);
        }

        /// <summary>
        /// Tests the case where assembly resolution should succeed.
        /// </summary>
        [TestMethod]
        public void AssemblyResolver_ResolutionByFileNameWithSpace_Succeeds()
        {
            // Arrante
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);
            Assembly testAssembly = CompileSimpleAssembly("Space in Name ByFileName.dll", testFolder, new TestLogger());

            // Act
            Assembly resolvedAssembly = AssertAssemblyLoadSucceedsOnlyWithResolver("Space in Name ByFileName.dll", testFolder);

            // Assert
            AssertExpectedAssemblyLoaded(testAssembly, resolvedAssembly);
        }

        /// <summary>
        /// Tests the case where assembly resolution should succeed.
        /// </summary>
        [TestMethod]
        public void AssemblyResolver_VersionAssemblyRequested()
        {
            // Arrange
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);
            Assembly testAssembly = CompileSimpleAssembly("VersionAsm1.dll", testFolder, new TestLogger(), "2.1.0.4");

            // Act
            Assembly resolvedAssembly = AssertAssemblyLoadSucceedsOnlyWithResolver("VersionAsm1, Version = 2.1.0.4, Culture = neutral, PublicKeyToken = null", testFolder);

            // Assert
            AssertExpectedAssemblyLoaded(testAssembly, resolvedAssembly);

            AssertAssemblyLoadFails("VersionAsm1, Version = 1.0.0.4, Culture = neutral, PublicKeyToken = null");

        }

        #endregion

        #region Private methods

        private Assembly CompileSimpleAssembly(string assemblyFileName, string asmFolder, ILogger logger, string version = "1.0.0.0")
        {
            Directory.CreateDirectory(asmFolder);
            string fullAssemblyFilePath = Path.Combine(asmFolder, assemblyFileName);

            return CompileAssembly(@"public class SimpleProgram {
              public static void Main(string[] args) {
                System.Console.WriteLine(""Hello World"");
              }
            }", fullAssemblyFilePath, version, logger);
        }

        /// <summary>
        /// Compiles the supplied code into a new assembly
        /// </summary>
        private static Assembly CompileAssembly(string code, string outputFilePath, string asmVersion, ILogger logger)
        {
            string versionedCode = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                @"[assembly:System.Reflection.AssemblyVersionAttribute(""{0}"")]
{1}", asmVersion, code);

            CompilerResults result = null;
            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                CompilerParameters options = new CompilerParameters();
                options.OutputAssembly = outputFilePath;
                options.GenerateExecutable = true;
                options.GenerateInMemory = false;

                result = provider.CompileAssemblyFromSource(options, versionedCode);

                if (result.Errors.Count > 0)
                {
                    foreach (string item in result.Output)
                    {
                        logger.LogInfo(item);
                    }
                    Assert.Fail("Test setup error: failed to create dynamic assembly. See the test output for compiler output");
                }
            }

            return result.CompiledAssembly;
        }

        #endregion

        #region Checks

        private static void AssertAssemblyLoadFails(string asmRef)
        {
            AssertException.Expect<FileNotFoundException>(() => Assembly.Load(asmRef));
        }

        private Assembly AssertAssemblyLoadSucceedsOnlyWithResolver(string asmRef, string searchPath)
        {
            // Check the assembly load fails without the assembly resolver
            AssertAssemblyLoadFails(asmRef);

            // Act
            Assembly resolveResult;

            // Create a test logger that will only record output from the resolver
            // so we can check it has been called
            using (AssemblyResolver resolver = new AssemblyResolver(new TestLogger(), searchPath))
            {
                resolveResult = Assembly.Load(asmRef);

                // Assert
                AssertResolverCaller(resolver);
            }

            // Assert
            Assert.IsNotNull(resolveResult, "Failed to the load the assembly");

            return resolveResult;
        }

        private static void AssertResolverCaller(AssemblyResolver resolver)
        {
            Assert.IsTrue(resolver.ResolverCalled, "Expected the assembly resolver to have been called");
        }

        private static void AssertExpectedAssemblyLoaded(Assembly expected, Assembly resolved)
        {
            Assert.IsNotNull(resolved, "Resolved assembly should not be null");
            Assert.AreEqual(expected.Location, resolved.Location, "Failed to load the expected assembly");
        }

        #endregion
    }
}
