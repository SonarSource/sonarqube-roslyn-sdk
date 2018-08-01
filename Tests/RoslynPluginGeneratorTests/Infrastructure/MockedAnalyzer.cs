using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynAnalyzer11;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests.Infrastructure
{
    /// <summary>
    /// A mock analyzer used for getting the correct assembly for loading the rule html desciptions.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, "Test#")]
    public class MockedAnalyzer : ConfigurableAnalyzer
    {
    }
}
