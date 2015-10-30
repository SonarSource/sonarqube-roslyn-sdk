using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace Roslyn.SonarQube
{
    /// <summary>
    /// Generates SonarQube rules from a Roslyn analyser
    /// </summary>
    public interface IRuleGenerator
    {
        /// <summary>
        /// Geneates SonarQube rules from a collection of Roslyn rules (aka diagnostics)
        /// </summary>
        Rules GenerateRules(IEnumerable<DiagnosticAnalyzer> diagnostics);
    }
}