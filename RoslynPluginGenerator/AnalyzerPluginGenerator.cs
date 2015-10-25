using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.SonarQube.Common;
using Roslyn.SonarQube.PluginGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Roslyn.SonarQube.AnalyzerPlugins
{
    public class AnalyzerPluginGenerator
    {
        public const string NuGetPackageSource = "https://www.nuget.org/api/v2/";

        private ILogger logger;

        public AnalyzerPluginGenerator(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.logger = logger;
        }

        public bool Generate(string nuGetPackageId, NuGet.SemanticVersion nuGetPackageVersion)
        {
            if (string.IsNullOrWhiteSpace(nuGetPackageId))
            {
                throw new ArgumentNullException("nuGetPackageId");
            }

            string tempPath = Path.Combine(Path.GetTempPath(), "AnalyserPlugins", Guid.NewGuid().ToString());

            NuGetPackageHandler downloader = new NuGetPackageHandler(logger);

            NuGet.IPackage package = downloader.FetchPackage(NuGetPackageSource, nuGetPackageId, nuGetPackageVersion, tempPath);

            if (package != null)
            {
                PluginDefinition pluginDefn = CreatePluginDefinition(package);

                string rulesFilePath = TryGenerateRulesFile(tempPath);

                if (rulesFilePath != null)
                {
                    this.logger.LogInfo(UIResources.APG_GeneratingPlugin);

                    string fullJarPath = Path.Combine(Directory.GetCurrentDirectory(), 
                        nuGetPackageId + "-plugin." + pluginDefn.Version + ".jar");
                    RulesPluginGenerator rulesPluginGen = new RulesPluginGenerator(logger);
                    rulesPluginGen.GeneratePlugin(pluginDefn, rulesFilePath, fullJarPath);

                    this.logger.LogInfo(UIResources.APG_PluginGenerated, fullJarPath);
                }
            }

            return package != null;
        }

        private string TryGenerateRulesFile(string tempDirectory)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingRules);
            string rulesFilePath = null;

            this.logger.LogInfo(UIResources.APG_LocatingAnalyzers);

            AnalyzerFinder finder = new AnalyzerFinder(this.logger);
            IEnumerable<DiagnosticAnalyzer> analyzers = finder.FindAnalyzers(tempDirectory);

            this.logger.LogInfo(UIResources.APG_AnalyzersLocated, analyzers.Count());

            if (analyzers.Any())
            {
                RuleGenerator ruleGen = new RuleGenerator();
                Rules rules = ruleGen.GenerateRules(analyzers);

                Debug.Assert(rules != null, "Not expecting the generated rules to be null");

                if (rules != null)
                {
                    rulesFilePath = Path.Combine(tempDirectory, "rules.xml");

                    rules.Save(rulesFilePath);
                    this.logger.LogDebug(UIResources.APG_RulesGeneratedToFile, rules.Count, rulesFilePath);
                }
            }
            else
            {
                this.logger.LogWarning(UIResources.APG_NoAnalyzersFound);
            }
            return rulesFilePath;
        }

        private static PluginDefinition CreatePluginDefinition(NuGet.IPackage package)
        {
            PluginDefinition pluginDefn = new PluginDefinition();

            pluginDefn.Description = GetValidManifestString(package.Description);
            pluginDefn.Developers = GetValidManifestString(ListToString(package.Authors));

            pluginDefn.Homepage = GetValidManifestString(package.ProjectUrl?.ToString());
            pluginDefn.Key = GetValidManifestString(package.Id);

            // TODO: hard-coded to C#
            pluginDefn.Language = "cs";
            pluginDefn.Name = GetValidManifestString(package.Title) ?? pluginDefn.Key;
            pluginDefn.Organization = GetValidManifestString(ListToString(package.Owners));
            pluginDefn.Version = GetValidManifestString(package.Version.ToNormalizedString());

            //pluginDefn.IssueTrackerUrl
            //pluginDefn.License;
            //pluginDefn.SourcesUrl;
            //pluginDefn.TermsConditionsUrl;

            return pluginDefn;
        }

        private static string GetValidManifestString(string value)
        {
            string valid = value;

            if (valid != null)
            {
                valid = valid.Replace('\r', ' ');
                valid = valid.Replace('\n', ' ');
            }
            return valid;
        }

        private static string ListToString(IEnumerable<string> args)
        {
            if (args == null)
            {
                return null;
            }
            return string.Join(",", args);
        }
    }
}
