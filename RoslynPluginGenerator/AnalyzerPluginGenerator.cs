//-----------------------------------------------------------------------
// <copyright file="AnalyzerPluginGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet;
using SonarQube.Plugins.Roslyn.CommandLine;
using SonarQube.Plugins.Roslyn;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SonarQube.Plugins.Roslyn
{
    public class AnalyzerPluginGenerator
    {
        public const string NuGetPackageSource = "https://www.nuget.org/api/v2/";

        /// <summary>
        /// The SARIF plugin must be able to distinguish the plugins generating SARIF style issues - a suffix convention is used
        /// </summary>
        private const string PluginKeySuffix = "_sarif";

        private readonly SonarQube.Plugins.Common.ILogger logger;

        public AnalyzerPluginGenerator(SonarQube.Plugins.Common.ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.logger = logger;
        }

        public bool Generate(NuGetReference analyzeRef, string sqaleFilePath)
        {
            // sqale file path is optional
            if (analyzeRef == null)
            {
                throw new ArgumentNullException("analyzeRef");
            }

            string baseDirectory = Path.Combine(
                Path.GetTempPath(),
                Assembly.GetEntryAssembly().GetName().Name);

            string nuGetDirectory = Path.Combine(baseDirectory, ".nuget");

            NuGetPackageHandler downloader = new NuGetPackageHandler(logger);

            IPackage package = downloader.FetchPackage(NuGetPackageSource, analyzeRef.PackageId, analyzeRef.Version, nuGetDirectory);

            if (package != null)
            {
                PluginDefinition pluginDefn = CreatePluginDefinition(package, sqaleFilePath);

                string outputDirectory = Path.Combine(baseDirectory, ".output", Guid.NewGuid().ToString());
                Directory.CreateDirectory(outputDirectory);

                string outputFilePath = Path.Combine(outputDirectory, "rules.xml");

                string packageDirectory = Path.Combine(nuGetDirectory, package.Id + "." + package.Version.ToString());
                Debug.Assert(Directory.Exists(packageDirectory), "Expected package directory does not exist: {0}", packageDirectory);
                bool success = TryGenerateRulesFile(packageDirectory, nuGetDirectory, outputFilePath);

                if (success)
                {
                    this.logger.LogInfo(UIResources.APG_GeneratingPlugin);

                    string fullJarPath = Path.Combine(Directory.GetCurrentDirectory(), 
                        analyzeRef.PackageId + "-plugin." + pluginDefn.Version + ".jar");
                    RulesPluginGenerator rulesPluginGen = new RulesPluginGenerator(logger);
                    rulesPluginGen.GeneratePlugin(pluginDefn, outputFilePath, fullJarPath);

                    this.logger.LogInfo(UIResources.APG_PluginGenerated, fullJarPath);
                }
            }

            return package != null;
        }

        /// <summary>
        /// Attempts to generate a rules file for assemblies in the package directory.
        /// Returns the path to the rules file.
        /// </summary>
        /// <param name="packageDirectory">Directory containing the analyzer assembly to generate rules for</param>
        /// <param name="nuGetDirectory">Directory containing other NuGet packages that might be required i.e. analyzer dependencies</param>
        private bool TryGenerateRulesFile(string packageDirectory, string nuGetDirectory, string outputFilePath)
        {
            bool success = false;
            this.logger.LogInfo(UIResources.APG_GeneratingRules);

            this.logger.LogInfo(UIResources.APG_LocatingAnalyzers);

            AnalyzerFinder finder = new AnalyzerFinder(this.logger);
            IEnumerable<DiagnosticAnalyzer> analyzers = finder.FindAnalyzers(packageDirectory, nuGetDirectory);

            this.logger.LogInfo(UIResources.APG_AnalyzersLocated, analyzers.Count());

            if (analyzers.Any())
            {
                RuleGenerator ruleGen = new RuleGenerator(this.logger);
                Rules rules = ruleGen.GenerateRules(analyzers);

                Debug.Assert(rules != null, "Not expecting the generated rules to be null");

                if (rules != null)
                {
                    rules.Save(outputFilePath, logger);
                    this.logger.LogDebug(UIResources.APG_RulesGeneratedToFile, rules.Count, outputFilePath);
                    success = true;
                }
            }
            else
            {
                this.logger.LogWarning(UIResources.APG_NoAnalyzersFound);
            }
            return success;
        }

        private static PluginDefinition CreatePluginDefinition(IPackage package, string sqaleFilePath)
        {
            PluginDefinition pluginDefn = new PluginDefinition();

            pluginDefn.Description = GetValidManifestString(package.Description);
            pluginDefn.Developers = GetValidManifestString(ListToString(package.Authors));

            pluginDefn.Homepage = GetValidManifestString(package.ProjectUrl?.ToString());
            pluginDefn.Key = GetValidManifestString(package.Id) + PluginKeySuffix;

            // TODO: hard-coded to C#
            pluginDefn.Language = "cs";
            pluginDefn.Name = GetValidManifestString(package.Title) ?? pluginDefn.Key;
            pluginDefn.Organization = GetValidManifestString(ListToString(package.Owners));
            pluginDefn.Version = GetValidManifestString(package.Version.ToNormalizedString());

            //pluginDefn.IssueTrackerUrl
            //pluginDefn.License;
            //pluginDefn.SourcesUrl;
            //pluginDefn.TermsConditionsUrl;

            if (!string.IsNullOrWhiteSpace(sqaleFilePath))
            {
                pluginDefn.AdditionalFileMap["resources/sqale.xml"] = sqaleFilePath;
            }
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
