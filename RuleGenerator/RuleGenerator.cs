using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roslyn.SonarQube
{
    /// <summary>
    /// Responsible for transforming Roslyn analyzer rule definitions to SonarQube rules format
    /// </summary>
    public class RuleGenerator : IRuleGenerator
    {
        public const string Cardinality = "SINGLE";
        public const string Status = "READY";
        public const string NoDescription = "No description";

        #region IRuleGenerator

        /// <summary>
        /// Generate SonarQube specifc rules based on Roslyn based diagnostics
        /// </summary>
        public Rules GenerateRules(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            if (analyzers == null)
            {
                throw new ArgumentNullException("analyzers");
            }

            Rules rules = new Rules();

            foreach (DiagnosticAnalyzer analyzer in analyzers)
            {
                rules.AddRange(GetAnalyzerRules(analyzer));
            }

            return rules;
        }

        #endregion IRuleGenerator

        #region Private methods

        private static Rules GetAnalyzerRules(DiagnosticAnalyzer analyzer)
        {
            // For info on SonarQube rules see http://docs.sonarqube.org/display/SONAR/Rules

            Rules rules = new Rules();

            foreach (DiagnosticDescriptor diagnostic in analyzer.SupportedDiagnostics)
            {
                Rule newRule = new Rule();
                newRule.Key = diagnostic.Id;
                newRule.InternalKey = diagnostic.Id;

                newRule.Description = diagnostic.Description.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(newRule.Description))
                {
                    newRule.Description = NoDescription;
                }

                newRule.Name = diagnostic.Title.ToString(System.Globalization.CultureInfo.InvariantCulture);
                newRule.Severity = GetSonarQubeSeverity(diagnostic.DefaultSeverity);

                // SonarQube tags have to be lower-case
                newRule.Tags = ExtractTags(diagnostic).Select(t => t.ToLowerInvariant()).ToArray();

                // Rule XML properties that don't have an obvious Diagnostic equivalent:
                newRule.Cardinality = Cardinality;
                newRule.Status = Status;

                // Diagnostic properties that don't have an obvious Rule xml equivalent:
                //diagnostic.HelpLinkUri;
                //diagnostic.Category;
                //diagnostic.IsEnabledByDefault;
                //diagnostic.MessageFormat;

                rules.Add(newRule);
            }
            return rules;
        }

        private static ISet<string> ExtractTags(DiagnosticDescriptor diagnostic)
        {
            ISet<string> tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (diagnostic.CustomTags.Any())
            {
                foreach (string tag in diagnostic.CustomTags.Where(t => !String.IsNullOrWhiteSpace(t)))
                {
                    if (tagSet.Contains(tag))
                    {
                        Console.WriteLine(Resources.WARN_DuplicateTags);
                    }
                    else
                    {
                        tagSet.Add(tag);
                    }
                }
            }

            return tagSet;
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

                case DiagnosticSeverity.Hidden:
                case DiagnosticSeverity.Info:
                default:
                    sqSeverity = "INFO";
                    break;
            }

            return sqSeverity;
        }

        #endregion Private methods
    }
}
