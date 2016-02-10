//-----------------------------------------------------------------------
// <copyright file="UnattributedAnalyzer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace RoslynAnalyzer11
{
    // Test class for SFSRAP-29: analyzers that don't have an [DiagnosticAnalyzer] attribute should be ignored
    // [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnattributedAnalyzer : DiagnosticAnalyzer
    {

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor("unattributed", "Title", "MessageFormat", "Testing", DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "Description");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
        }

    }
}
