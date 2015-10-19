using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;

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
            Rules rules = new Rules();

            foreach(DiagnosticDescriptor diagnostic in analyzer.SupportedDiagnostics)
            {
                rule newRule = new rule();
                newRule.Key = diagnostic.Id;
                newRule.InternalKey = diagnostic.Id;
                newRule.Description = diagnostic.Description.ToString(System.Globalization.CultureInfo.InvariantCulture);
                newRule.Name = diagnostic.Title.ToString(System.Globalization.CultureInfo.InvariantCulture);
                newRule.Severity = diagnostic.DefaultSeverity.ToString();

                //newRule.Cardinality = diagnostic;
                //newRule.Status = diagnostic;
                //newRule.Tag = ;

                //diagnostic.HelpLinkUri;
                //diagnostic.Category;
                //diagnostic.CustomTags;
                //diagnostic.IsEnabledByDefault;
                //diagnostic.MessageFormat;


                rules.Add(newRule);
            }
            return rules;
        }

        #endregion

    }
}
