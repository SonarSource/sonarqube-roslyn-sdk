using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.SonarQube
{
    /// <summary>
    /// Generates SonarQube rules from a Roslyn analyser
    /// </summary>
    public interface IRuleGenerator
    {
        Rules GenerateRules(IEnumerable<DiagnosticAnalyzer> analyzers);
    }
}
