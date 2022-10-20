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

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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
            string description = DefaultDescription,
            string helpLinkUri = "",
            string[] tags = null)
        {
            var diagnostic = new DiagnosticDescriptor(key, title, messageFormat, category, severity, isEnabledByDefault, description, helpLinkUri, tags);
            registeredDiagnostics.Add(diagnostic);

            return diagnostic;
        }

        public void ResetDiagnostics()
        {
            registeredDiagnostics.Clear();
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

        #endregion Boilerplate code
    }
}