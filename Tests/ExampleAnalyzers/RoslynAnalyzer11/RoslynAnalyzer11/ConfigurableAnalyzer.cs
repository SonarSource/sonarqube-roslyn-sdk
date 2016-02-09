//-----------------------------------------------------------------------
// <copyright file="ConfigurableAnalyzer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace RoslynAnalyzer11
{
    /// <summary>
    /// Configurable analyzer. Use the static methods before instantiating it. Note that loading the test assembly and reflecting
    /// over it will not produce any rules from this analyzer.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic, "Test#")]
    public class ConfigurableAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ConfigurableAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        private const string DefaultTitle = "Type names should be all uppercase.";

        private const string DefaultMessageFormat = "Type name '{0}' contains lowercase letters";
        private const string DefaultDescription = "Type names should be all uppercase.";
        private const string DefaultCategory = "Naming";

        #region Test interface

        private readonly List<DiagnosticDescriptor> registeredDiagnostics = new List<DiagnosticDescriptor>();

        public DiagnosticDescriptor RegisterDiagnostic(
            string key,
            string title = DefaultTitle,
            string messageFormat = DefaultMessageFormat,
            string category = DefaultCategory,
            DiagnosticSeverity severity = DiagnosticSeverity.Warning,
            bool isEnabledByDefault = true,
            string description = "",
            string helpLinkUri = "",
            string[] tags = null)
        {
            var diagnostic = new DiagnosticDescriptor(key, title, messageFormat, category, severity, isEnabledByDefault, description, helpLinkUri, tags);
            this.registeredDiagnostics.Add(diagnostic);

            return diagnostic;
        }

        public void ResetDiagnostics()
        {
            this.registeredDiagnostics.Clear();
        }

        #endregion Test interface

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(registeredDiagnostics.ToArray());
            }
        }

        #region Boilerplate code

        public override void Initialize(AnalysisContext context)
        {
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
        }

        #endregion Boilerplate code
    }
}
