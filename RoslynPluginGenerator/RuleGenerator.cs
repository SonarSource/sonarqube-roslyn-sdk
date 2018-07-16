/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarQube.Plugins.Common;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Responsible for transforming Roslyn analyzer rule definitions to SonarQube rules format
    /// </summary>
    public class RuleGenerator : IRuleGenerator
    {
        public const string Cardinality = "SINGLE";
        public const string Status = "READY";
        private readonly ILogger logger;

        public RuleGenerator(ILogger logger)
        {
            this.logger = logger;
        }

        #region IRuleGenerator

        /// <summary>
        /// Generate SonarQube specific rules based on Roslyn based diagnostics
        /// </summary>
        public Rules GenerateRules(IEnumerable<DiagnosticAnalyzer> diagnostics)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            Rules rules = new Rules();

            foreach (DiagnosticAnalyzer analyzer in diagnostics)
            {
                Rules analyzerRules = GetAnalyzerRules(analyzer);

                foreach (Rule analyzerRule in analyzerRules)
                {
                    if (rules.Any(r => String.Equals(r.Key, analyzerRule.Key, Rule.RuleKeyComparer)))
                    {
                        logger.LogWarning(UIResources.RuleGen_DuplicateKey, analyzerRule.Key);
                        continue;
                    }

                    rules.Add(analyzerRule);
                }
            }

            return rules;
        }

        #endregion IRuleGenerator

        #region Private methods

        private Rules GetAnalyzerRules(DiagnosticAnalyzer analyzer)
        {
            // For info on SonarQube rules see http://docs.sonarqube.org/display/SONAR/Rules

            Rules rules = new Rules();

            foreach (DiagnosticDescriptor diagnostic in analyzer.SupportedDiagnostics)
            {
                if (String.IsNullOrWhiteSpace(diagnostic.Id))
                {
                    logger.LogWarning(UIResources.RuleGen_EmptyKey, analyzer.ToString());
                    continue;
                }

                Rule newRule = new Rule
                {
                    Key = diagnostic.Id,
                    InternalKey = diagnostic.Id,

                    Description = GetDescriptionAsRawHtml(diagnostic),

                    Name = diagnostic.Title.ToString(CultureInfo.InvariantCulture),
                    Severity = GetSonarQubeSeverity(diagnostic.DefaultSeverity),

                    // Rule XML properties that don't have an obvious Diagnostic equivalent:
                    Cardinality = Cardinality,
                    Status = Status
                };

                // Diagnostic properties that don't have an obvious Rule xml equivalent:
                //  diagnostic.Category
                //  diagnostic.IsEnabledByDefault
                //  diagnostic.MessageFormat

                /* Remark: Custom tags are used so that Visual Studio handles diagnostics and are not equivalent to SonarQube's tags
                *
                * http://stackoverflow.com/questions/24257222/relevance-of-new-parameters-for-diagnosticdescriptor-constructor
                * customTags is a general way to mark that a diagnostic should be treated or displayed somewhat
                * different than normal diagnostics. The "unnecessary" tag means that in the IDE we fade out the span
                * that the diagnostic applies to: this is how we fade out unnecessary usings or casts or such in the IDE.
                * In some fancy scenarios you might want to define your own, but for the most part you'll either leave that empty
                * or pass Unnecessary if you want the different UI handling.
                * The EditAndContinue tag is for errors that are created if an edit-and-continue edit can't be applied
                * (which are also displayed somewhat differently)...that's just for us (n.b. Roslyn) to use.
                */

                rules.Add(newRule);
            }
            return rules;
        }

        /// <summary>
        /// Returns the description as HTML
        /// </summary>
        /// <returns>Note: the description should be returned as the HTML that should be rendered i.e. there is no need enclose it in a CDATA section</returns>
        private static string GetDescriptionAsRawHtml(DiagnosticDescriptor diagnostic)
        {
            StringBuilder sb = new StringBuilder();
            bool hasDescription = false;

            string details = diagnostic.Description.ToString(CultureInfo.CurrentCulture);
            if (!String.IsNullOrWhiteSpace(details))
            {
                sb.AppendLine("<p>" + details + "</p>");
                hasDescription = true;
            }

            if (!String.IsNullOrWhiteSpace(diagnostic.HelpLinkUri))
            {
                sb.AppendLine("<h2>" + UIResources.RuleGen_MoreDetailsTitle + "</h2>");
                sb.AppendLine(String.Format(UIResources.RuleGen_ForMoreDetailsLink, diagnostic.HelpLinkUri));
                hasDescription = true;
            }

            if (!hasDescription)
            {
                return UIResources.RuleGen_NoDescription;
            }

            return sb.ToString();
        }

        private static string GetSonarQubeSeverity(DiagnosticSeverity diagnosticSeverity)
        {
            // TODO: decide on appropriate severities mappings

            // SonarQube severities: Blocker, Critical, Major, Minor, Info
            // Roslyn Diagnostic severities: Error, Warning, Hidden, Info
            string sqSeverity;

            switch (diagnosticSeverity)
            {
                case DiagnosticSeverity.Error:
                    sqSeverity = "MAJOR";
                    break;

                case DiagnosticSeverity.Warning:
                    sqSeverity = "MINOR";
                    break;

                default:
                    sqSeverity = "INFO";
                    break;
            }

            return sqSeverity;
        }

        #endregion Private methods
    }
}