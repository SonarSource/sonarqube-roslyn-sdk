//-----------------------------------------------------------------------
// <copyright file="DiagnosticAssemblyScannerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using ExampleAnalyzer1;
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
        public void ScanDirectoryWithNoAnalyzerAssemblies()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

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

        [TestMethod]
        public void ScanValidAssembly()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);
            string validAnalyzerAssemblyPath = typeof(SimpleAnalyzer).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> csharpDiagnostics = scanner.InstantiateDiagnosticsFromAssembly(validAnalyzerAssemblyPath, LanguageNames.CSharp);
            IEnumerable<DiagnosticAnalyzer> vbDiagnostics = scanner.InstantiateDiagnosticsFromAssembly(validAnalyzerAssemblyPath, LanguageNames.VisualBasic);

            //Assert
            // ConfigurableAnalyzer is both C# and VB, so should appear in both
            Assert.AreEqual(2, csharpDiagnostics.Count(), "Expecting 2 C# analyzers");
            Assert.AreEqual(2, vbDiagnostics.Count(), "Expecting 2 VB analyzers");

            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d is SimpleAnalyzer),
                "Expected a SimpleAnalyzer");

            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d is ConfigurableAnalyzer),
                "Expected a ConfigurableAnalyzer");

            Assert.IsNotNull(
                vbDiagnostics.SingleOrDefault(d => d is VBAnalyzer),
                "Expected a VBAnalyzer");

            Assert.IsNull(
                vbDiagnostics.SingleOrDefault(d => d is AbstractAnalyzer),
                "Expected no abstract analyzers");
        }

        [TestMethod]
        public void ScanNonAnalyzerAssembly()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);
            string noAnalyzerAssemblyPath = typeof(DiagnosticAssemblyScannerTests).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> csharpDiagnostics = scanner.InstantiateDiagnosticsFromAssembly(noAnalyzerAssemblyPath, LanguageNames.CSharp);
            IEnumerable<DiagnosticAnalyzer> vbDiagnostics = scanner.InstantiateDiagnosticsFromAssembly(noAnalyzerAssemblyPath, LanguageNames.VisualBasic);

            // Assert
            Assert.AreEqual(0, csharpDiagnostics.Count(), "No analyzers should have been detected");
            Assert.AreEqual(0, vbDiagnostics.Count(), "No analyzers should have been detected");
        }
    }
}
