using ExampleAnalyzer1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Common;

namespace RuleGeneratorTests
{
    [TestClass]
    public class DiagnosticAssemblyScannerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ScanValidAssembly()
        {
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner();
            string validAnalyserAssemblyPath = typeof(SimpleAnalyzer).Assembly.Location;

            var diagnostics = scanner.ExtractDiagnosticsFromAssembly(validAnalyserAssemblyPath);
            Assert.AreEqual(2, diagnostics.Count(), "Expecting 2 analyzers");

            Assert.IsNotNull(
                diagnostics.SingleOrDefault(d => d is SimpleAnalyzer && d.SupportedDiagnostics.Count() == 1),
                "Expecting to find a SimpleAnalyzer with a single diagnostic");

            Assert.IsNotNull(
                diagnostics.SingleOrDefault(d => d is ConfigurableAnalyzer && !d.SupportedDiagnostics.Any()),
                "Expecting to find a ConfigurableAnalyzer with no diagnostics");
        }

        [TestMethod]
        public void ScanNonAnalyzerAssembly()
        {
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner();
            string validAnalyserAssemblyPath = typeof(DiagnosticAssemblyScannerTests).Assembly.Location;

            var diagnostics = scanner.ExtractDiagnosticsFromAssembly(validAnalyserAssemblyPath);
            Assert.AreEqual(0, diagnostics.Count(), "No analsers should be detected");

           
        }
    }
}
