using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roslyn.SonarQube
{
    public class RuleGenerator : IRuleGenerator
    {
        #region IRuleGenerator

        public Rules GenerateRules(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            if (analyzers == null)
            {
                throw new ArgumentNullException("analyzers");
            }

            Rules allRules = new Rules();

            foreach(DiagnosticAnalyzer analyzer in analyzers)
            {
                allRules.AddRange(GetAnalyzerRules(analyzer));
            }

            return allRules;
        }

        #endregion

        #region Private methods

        private static Rules GetAnalyzerRules(DiagnosticAnalyzer analyzer)
        {
            // For info on SonarQube rules see http://docs.sonarqube.org/display/SONAR/Rules

            Rules rules = new Rules();

            foreach(DiagnosticDescriptor diagnostic in analyzer.SupportedDiagnostics)
            {
                Rule newRule = new Rule();
                newRule.Key = diagnostic.Id;
                newRule.InternalKey = diagnostic.Id;
                newRule.Description = diagnostic.Description.ToString(System.Globalization.CultureInfo.InvariantCulture);
                newRule.Name = diagnostic.Title.ToString(System.Globalization.CultureInfo.InvariantCulture);
                newRule.Severity = GetSonarQubeSeverity(diagnostic.DefaultSeverity);
                
                if (diagnostic.CustomTags.Any())
                {
                    var tags = new List<string>();
                    foreach (string tag in diagnostic.CustomTags)
                    {
                        tags.Add(tag);
                    }
                    newRule.Tags = tags.ToArray();
                }

                // Rule XML properties that don't have an obvious Diagnostic equivalent:
                newRule.Cardinality = "SINGLE";
                newRule.Status = "READY";

                // Diagnostic properties that don't have an obvious Rule xml equivalent:
                //diagnostic.HelpLinkUri;
                //diagnostic.Category;
                //diagnostic.IsEnabledByDefault;
                //diagnostic.MessageFormat;


                rules.Add(newRule);
            }
            return rules;
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

        #endregion
    }
}
