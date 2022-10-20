/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2022 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS0414 // Value is assigned but never used
        // Referencing this forces some commonly-used libraries to be loaded
        private static readonly LanguageVersion csVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp1;
#pragma warning restore CS0414 // Value is assigned but never used
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore S1144 // Unused private types or members should be removed

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
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