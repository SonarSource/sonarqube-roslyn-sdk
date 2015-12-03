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
using Roslyn.SonarQube;
using System.Collections.Generic;
using System.Linq;
using Tests.Common;
using TestUtilities;

namespace RuleGeneratorTests
{
    [TestClass]
    public class DiagnosticAssemblyScannerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ScanValidAssembly()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);
            string validAnalyserAssemblyPath = typeof(SimpleAnalyzer).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> csharpDiagnostics = scanner.InstantiateDiagnosticsFromAssembly(validAnalyserAssemblyPath, LanguageNames.CSharp);
            IEnumerable<DiagnosticAnalyzer> vbDiagnostics = scanner.InstantiateDiagnosticsFromAssembly(validAnalyserAssemblyPath, LanguageNames.VisualBasic);

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
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);
            string validAnalyserAssemblyPath = typeof(DiagnosticAssemblyScannerTests).Assembly.Location;

            var diagnostics = scanner.InstantiateDiagnosticsFromAssembly(validAnalyserAssemblyPath, LanguageNames.CSharp);
            Assert.AreEqual(0, diagnostics.Count(), "No analyzers should have been detected");
        }
    }
}
