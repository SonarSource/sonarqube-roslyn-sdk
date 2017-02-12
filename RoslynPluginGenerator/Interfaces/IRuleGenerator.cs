//-----------------------------------------------------------------------
// <copyright file="IRuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Generates SonarQube rules from a Roslyn analyzer
    /// </summary>
    public interface IRuleGenerator
    {
        /// <summary>
        /// Generates SonarQube rules from a collection of Roslyn rules (aka diagnostics)
        /// </summary>
        Rules GenerateRules(IEnumerable<DiagnosticAnalyzer> diagnostics, UserOptions userOptions);
    }
}