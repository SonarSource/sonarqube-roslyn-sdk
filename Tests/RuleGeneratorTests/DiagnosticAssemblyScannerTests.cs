using ExampleAnalyzer1;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube;
using System.Linq;
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
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);
            string validAnalyserAssemblyPath = typeof(SimpleAnalyzer).Assembly.Location;

            var csharpDiagnostics = scanner.ExtractDiagnosticsFromAssembly(validAnalyserAssemblyPath, LanguageNames.CSharp);
            var vbDiagnostics = scanner.ExtractDiagnosticsFromAssembly(validAnalyserAssemblyPath, LanguageNames.VisualBasic);

            Assert.AreEqual(2, csharpDiagnostics.Count(), "Expecting 2 C# analyzers");
            Assert.AreEqual(2, csharpDiagnostics.Count(), "Expecting 1 VB analyzer");

            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d is SimpleAnalyzer && d.SupportedDiagnostics.Count() == 1),
                "Expecting to find a SimpleAnalyzer with a single diagnostic");

            Assert.IsNotNull(
                csharpDiagnostics.SingleOrDefault(d => d is ConfigurableAnalyzer && !d.SupportedDiagnostics.Any()),
                "Expecting to find a ConfigurableAnalyzer with no diagnostics");

            Assert.IsNotNull(
                vbDiagnostics.SingleOrDefault(d => d is VBAnalyzer && d.SupportedDiagnostics.Count() == 1),
                "Expecting to find a VBAnalyzer with a single diagnostic");

        }

        [TestMethod]
        public void ScanNonAnalyzerAssembly()
        {
            TestLogger logger = new TestLogger();
            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);
            string validAnalyserAssemblyPath = typeof(DiagnosticAssemblyScannerTests).Assembly.Location;

            var diagnostics = scanner.ExtractDiagnosticsFromAssembly(validAnalyserAssemblyPath, LanguageNames.CSharp);
            Assert.AreEqual(0, diagnostics.Count(), "No analyzers should have been detected");
        }
    }
}