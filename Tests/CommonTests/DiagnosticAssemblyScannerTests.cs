//-----------------------------------------------------------------------
// <copyright file="DiagnosticAssemblyScannerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using ExampleAnalyzer1;
using ExampleAnalyzer2;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Roslyn;
using System.Collections.Generic;
using System.Linq;
using SonarQube.Plugins.Test.Common;
using System.IO;

namespace SonarQube.Plugins.Roslyn.RuleGeneratorTests
{
    [TestClass]
    public class DiagnosticAssemblyScannerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DeploymentItem("ExampleAnalyzer1.dll", "./oneAnalyzer")]
        public void ScanDirectory_OneAnalyzerAssembly()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string testDirectoryPath = Path.Combine(TestContext.DeploymentDirectory, "oneAnalyzer");

            string exampleAnalyzer1Path = Path.Combine(testDirectoryPath, "ExampleAnalyzer1.dll");
            Assert.IsTrue(File.Exists(exampleAnalyzer1Path), "Test setup error: expected assembly does not exist: {0}", exampleAnalyzer1Path);

            // Act
            IEnumerable<DiagnosticAnalyzer> csharpDiagnostics = scanner.InstantiateDiagnostics(testDirectoryPath, LanguageNames.CSharp);
            IEnumerable<DiagnosticAnalyzer> vbDiagnostics = scanner.InstantiateDiagnostics(testDirectoryPath, LanguageNames.VisualBasic);

            // Assert
            // ConfigurableAnalyzer is both C# and VB, so should appear in both
            Assert.AreEqual(2, csharpDiagnostics.Count(), "Expecting 2 C# analyzers");
            Assert.AreEqual(2, vbDiagnostics.Count(), "Expecting 2 VB analyzers");

            // Using name comparison because type comparison fails if the types are from assemblies with different paths (even if copied)
            // Loaded from ExampleAnalyzer1.dll
            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(SimpleAnalyzer).FullName),
                "Expected a SimpleAnalyzer");

            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(ConfigurableAnalyzer).FullName),
                "Expected a ConfigurableAnalyzer");
            
            Assert.IsNotNull(
                vbDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(VBAnalyzer).FullName),
                "Expected a VBAnalyzer");

            Assert.IsNotNull(
                vbDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(ConfigurableAnalyzer).FullName),
                "Expected a ConfigurableAnalyzer");

            Assert.IsNull(
                vbDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(AbstractAnalyzer).FullName),
                "Expected no abstract analyzers");
        }

        [TestMethod]
        [DeploymentItem("ExampleAnalyzer1.dll", "./twoAnalyzers")]
        [DeploymentItem("ExampleAnalyzer2.dll", "./twoAnalyzers")]
        public void ScanDirectory_MultipleAnalyzerAssemblies()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string testDirectoryPath = Path.Combine(TestContext.DeploymentDirectory, "twoAnalyzers");

            string exampleAnalyzer1Path = Path.Combine(testDirectoryPath, "ExampleAnalyzer1.dll");
            Assert.IsTrue(File.Exists(exampleAnalyzer1Path), "Test setup error: expected assembly does not exist: {0}", exampleAnalyzer1Path);
            string exampleAnalyzer2Path = Path.Combine(testDirectoryPath, "ExampleAnalyzer2.dll");
            Assert.IsTrue(File.Exists(exampleAnalyzer2Path), "Test setup error: expected assembly does not exist: {0}", exampleAnalyzer2Path);


            // Act
            IEnumerable<DiagnosticAnalyzer> csharpDiagnostics = scanner.InstantiateDiagnostics(testDirectoryPath, LanguageNames.CSharp);
            IEnumerable<DiagnosticAnalyzer> vbDiagnostics = scanner.InstantiateDiagnostics(testDirectoryPath, LanguageNames.VisualBasic);

            // Assert
            // ConfigurableAnalyzer is both C# and VB, so should appear in both
            Assert.AreEqual(3, csharpDiagnostics.Count(), "Expecting 3 C# analyzers");
            Assert.AreEqual(2, vbDiagnostics.Count(), "Expecting 2 VB analyzers");

            // Using name comparison because type comparison fails if the types are from assemblies with different paths (even if copied)
            // Loaded from ExampleAnalyzer1.dll
            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(SimpleAnalyzer).FullName),
                "Expected a SimpleAnalyzer");

            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(ConfigurableAnalyzer).FullName),
                "Expected a ConfigurableAnalyzer");

            Assert.IsNotNull(
                vbDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(VBAnalyzer).FullName),
                "Expected a VBAnalyzer");

            Assert.IsNotNull(
                vbDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(ConfigurableAnalyzer).FullName),
                "Expected a ConfigurableAnalyzer");

            Assert.IsNull(
                vbDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(AbstractAnalyzer).FullName),
                "Expected no abstract analyzers");

            // Loaded from ExampleAnalyzer2.dll
            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d.GetType().FullName == typeof(ExampleAnalyzer).FullName),
                "Expected an ExampleAnalyzer");
        }

        [TestMethod]
        public void ScanDirectoryWithNoAnalyzerAssemblies()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            // place a single assembly in the test directory, that does not have any analyzers in it
            string noAnalyzerAssemblyPath = typeof(DiagnosticAssemblyScannerTests).Assembly.Location;
            string testDirectoryPath = TestUtils.CreateTestDirectory(this.TestContext);
            string testAssemblyPath = Path.Combine(testDirectoryPath, Path.GetFileName(noAnalyzerAssemblyPath));
            File.Copy(noAnalyzerAssemblyPath, testAssemblyPath);

            // Act
            IEnumerable<DiagnosticAnalyzer> csharpDiagnostics = scanner.InstantiateDiagnostics(testDirectoryPath, LanguageNames.CSharp);
            IEnumerable<DiagnosticAnalyzer> vbDiagnostics = scanner.InstantiateDiagnostics(testDirectoryPath, LanguageNames.VisualBasic);

            // Assert
            Assert.AreEqual(0, csharpDiagnostics.Count(), "No analyzers should have been detected");
            Assert.AreEqual(0, vbDiagnostics.Count(), "No analyzers should have been detected");
        }
    }
}
