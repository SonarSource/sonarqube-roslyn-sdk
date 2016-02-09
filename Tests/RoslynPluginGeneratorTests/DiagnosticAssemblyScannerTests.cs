//-----------------------------------------------------------------------
// <copyright file="DiagnosticAssemblyScannerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using SonarQube.Plugins.Test.Common;
using System.IO;
using System;

namespace SonarQube.Plugins.Roslyn.RuleGeneratorTests
{
    [TestClass]
    public class DiagnosticAssemblyScannerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void InstantiateDiags_CSharp_NoFiles()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.CSharp /* no files */);

            // Assert
            Assert.IsNotNull(result, "Not expecting InstantiateDiagnostics to return null");
            Assert.IsFalse(result.Any(), "Not expecting any diagnostics to have been found");
        }

        [TestMethod]
        public void InstantiateDiags_VB_NoAnalyzers()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string corLibDllPath = typeof(object).Assembly.Location;
            string thisDllPath = this.GetType().Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.VisualBasic,
                corLibDllPath,
                thisDllPath);

            // Assert
            Assert.IsNotNull(result, "Not expecting InstantiateDiagnostics to return null");
            Assert.IsFalse(result.Any(), "Not expecting any diagnostics to have been found");
        }

        [TestMethod]
        public void InstantiateDiags_CSharp_AnalyzersFound()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger, this.TestContext.DeploymentDirectory);

            string roslynAnalyzer11DllPath = typeof(RoslynAnalyzer11.CSharpAnalyzer).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.CSharp, roslynAnalyzer11DllPath);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer11.CSharpAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer11.ConfigurableAnalyzer));
            Assert_AnalyzerNotPresent(result, typeof(RoslynAnalyzer11.AbstractAnalyzer)); // not expecting abstract analyzers

            Assert.AreEqual(2, result.Count(), "Expecting 2 C# analyzers");
        }

        [TestMethod]
        public void InstantiateDiags_VB_AnalyzersFound()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string roslynAnalyzer11DllPath = typeof(RoslynAnalyzer11.CSharpAnalyzer).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.VisualBasic, roslynAnalyzer11DllPath);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer11.VBAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer11.ConfigurableAnalyzer));
            Assert_AnalyzerNotPresent(result, typeof(RoslynAnalyzer11.AbstractAnalyzer)); // not expecting abstract analyzers

            Assert.AreEqual(2, result.Count(), "Expecting 2 VB analyzers");
        }

        [TestMethod]
        public void InstantiateDiags_MultipleAssemblies_AnalyzersFound()
        {
            // This test expects that we can load analyzers from multiple paths at once.
            // SFSRAP-26: We should be able to load analyzers compiled using both Roslyn 1.1 and 1.0
            // (RoslynAnalyzer11 and RoslynAnalyzer10, respectively)

            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string roslynAnalyzer11DllPath = typeof(RoslynAnalyzer11.CSharpAnalyzer).Assembly.Location;
            string nonAnalyzerAssemblyPath = this.GetType().Assembly.Location;
            string roslynAnalyzer10DllPath = typeof(RoslynAnalyzer10.ExampleAnalyzer2).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.CSharp,
                roslynAnalyzer11DllPath,
                nonAnalyzerAssemblyPath,
                roslynAnalyzer10DllPath);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer11.CSharpAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer11.ConfigurableAnalyzer));
            Assert_AnalyzerNotPresent(result, typeof(RoslynAnalyzer11.AbstractAnalyzer)); // not expecting abstract analyzers

            Assert_AnalyzerIsPresent(result, typeof(RoslynAnalyzer10.ExampleAnalyzer2));

            Assert.AreEqual(3, result.Count(), "Unexpected number of C# analyzers returned");
        }

        #region Private Methods

        private void Assert_AnalyzerIsPresent(IEnumerable<DiagnosticAnalyzer> analyzers, Type expected)
        {
            string analyzerName = expected.FullName;
            Assert.IsNotNull(
                analyzers.SingleOrDefault(d => d.GetType().FullName == analyzerName),
                "Expected an analyzer with name: " + analyzerName);
        }
        private void Assert_AnalyzerNotPresent(IEnumerable<DiagnosticAnalyzer> analyzers, Type expected)
        {
            string analyzerName = expected.FullName;
            Assert.IsNull(
                analyzers.SingleOrDefault(d => d.GetType().FullName == analyzerName),
                "Expected no analyzers with name: " + analyzerName);
        }

        #endregion
    }
}
