//-----------------------------------------------------------------------
// <copyright file="DiagnosticAnalyzer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace RoslynAnalyzer10
{
    /// <summary>
    /// Roslyn analyzer for testing purposes only.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExampleAnalyzer2 : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ExampleAnalyzer2";

        private static readonly LocalizableString Title = "ExampleAnalyzer2 Title";
        private static readonly LocalizableString MessageFormat = "ExampleAnalyzer2 MessageFormat";
        private static readonly LocalizableString Description = "ExampleAnalyzer2 Description";
        private const string Category = "Naming";

        // Referencing this forces some commonly-used libraries to be loaded
        private static readonly LanguageVersion csVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp1;

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Report issues against symbols called "FISH"
            if (namedTypeSymbol.Name.Equals("FISH", StringComparison.Ordinal))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
