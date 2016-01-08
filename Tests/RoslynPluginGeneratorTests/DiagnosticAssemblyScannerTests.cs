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

            string exampleAnalyzer1DllPath = typeof(ExampleAnalyzer1.CSharpAnalyzer).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.CSharp, exampleAnalyzer1DllPath);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(ExampleAnalyzer1.CSharpAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(ExampleAnalyzer1.ConfigurableAnalyzer));
            Assert_AnalyzerNotPresent(result, typeof(ExampleAnalyzer1.AbstractAnalyzer)); // not expecting abstract analyzers

            Assert.AreEqual(2, result.Count(), "Expecting 2 C# analyzers");
        }

        [TestMethod]
        public void InstantiateDiags_VB_AnalyzersFound()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string exampleAnalyzer1DllPath = typeof(ExampleAnalyzer1.CSharpAnalyzer).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.VisualBasic, exampleAnalyzer1DllPath);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(ExampleAnalyzer1.VBAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(ExampleAnalyzer1.ConfigurableAnalyzer));
            Assert_AnalyzerNotPresent(result, typeof(ExampleAnalyzer1.AbstractAnalyzer)); // not expecting abstract analyzers

            Assert.AreEqual(2, result.Count(), "Expecting 2 VB analyzers");
        }

        [TestMethod]
        public void InstantiateDiags_MultipleAssemblies_AnalyzersFound()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);

            string exampleAnalyzer1DllPath = typeof(ExampleAnalyzer1.CSharpAnalyzer).Assembly.Location;
            string nonAnalyzerAssemblyPath = this.GetType().Assembly.Location;
            string exampleAnalyzer2DllPath = typeof(ExampleAnalyzer2.ExampleAnalyzer2).Assembly.Location;

            // Act
            IEnumerable<DiagnosticAnalyzer> result = scanner.InstantiateDiagnostics(LanguageNames.CSharp,
                exampleAnalyzer1DllPath,
                nonAnalyzerAssemblyPath,
                exampleAnalyzer2DllPath);

            // Assert
            Assert_AnalyzerIsPresent(result, typeof(ExampleAnalyzer1.CSharpAnalyzer));
            Assert_AnalyzerIsPresent(result, typeof(ExampleAnalyzer1.ConfigurableAnalyzer));
            Assert_AnalyzerNotPresent(result, typeof(ExampleAnalyzer1.AbstractAnalyzer)); // not expecting abstract analyzers

            Assert_AnalyzerIsPresent(result, typeof(ExampleAnalyzer2.ExampleAnalyzer2));

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
