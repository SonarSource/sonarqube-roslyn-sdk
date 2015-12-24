//-----------------------------------------------------------------------
// <copyright file="AnalyzerPluginGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet;
using SonarQube.Plugins.Roslyn.CommandLine;
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

        private const string RoslynResourcesRoot = "SonarQube.Plugins.Roslyn.Resources.";

        /// <summary>
        /// List of extensions (property definitions) that are added in the Java source code
        /// </summary>
        private static readonly string[] Extensions = new string[]
        {
            "RoslynProperties.AnalyzerId",
            "RoslynProperties.RuleNamespace",
            "RoslynProperties.NuGetPackageId",
            "RoslynProperties.NuGetPackageVersion"
        };

        private const string AnalyzerId_Token = "[ROSLYN_ANALYZER_ID]";
        private const string RuleNamespace_Token = "[ROSLYN_RULE_NAMESPACE]";
        private const string PackageId_Token = "[ROSLYN_NUGET_PACKAGE_ID]";
        private const string PackageVersion_Token = "[ROSLYN_NUGET_PACKAGE_VERSION]";

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
                // TODO: support multiple languages
                string language = SupportedLanguages.CSharp;
                PluginManifest pluginDefn = CreatePluginDefinition(package);

                string outputDirectory = Path.Combine(baseDirectory, ".output", Guid.NewGuid().ToString());
                Directory.CreateDirectory(outputDirectory);

                string rulesFilePath = Path.Combine(outputDirectory, "rules.xml");

                // TODO: we shouldn't try to work out where the content files have been installed by NuGet.
                // Instead, we should use the methods on IPackage to locate the assemblies e.g. Package.GetFiles()
                string packageDirectory = Path.Combine(nuGetDirectory, package.Id + "." + package.Version.ToString());
                Debug.Assert(Directory.Exists(packageDirectory), "Expected package directory does not exist: {0}", packageDirectory);

                bool success = TryGenerateRulesFile(packageDirectory, nuGetDirectory, rulesFilePath);

                if (success)
                {
                    BuildPlugin(analyzeRef, sqaleFilePath, language, pluginDefn, rulesFilePath, outputDirectory, package);
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

            DiagnosticAssemblyScanner diagnosticAssemblyScanner = new DiagnosticAssemblyScanner(this.logger, nuGetDirectory);
            IEnumerable<DiagnosticAnalyzer> analyzers = diagnosticAssemblyScanner.InstantiateDiagnostics(packageDirectory, LanguageNames.CSharp);

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

        private static PluginManifest CreatePluginDefinition(IPackage package)
        {
            PluginManifest pluginDefn = new PluginManifest();

            pluginDefn.Description = GetValidManifestString(package.Description);
            pluginDefn.Developers = GetValidManifestString(ListToString(package.Authors));

            pluginDefn.Homepage = GetValidManifestString(package.ProjectUrl?.ToString());
            pluginDefn.Key = GetValidManifestString(package.Id) + PluginKeySuffix;

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

        private void BuildPlugin(NuGetReference analyzeRef, string sqaleFilePath, string language, PluginManifest pluginDefn, string rulesFilePath, string tempDirectory, IPackage package)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingPlugin);

            string fullJarPath = Path.Combine(Directory.GetCurrentDirectory(),
                analyzeRef.PackageId + "-plugin." + pluginDefn.Version + ".jar");

            PluginBuilder builder = new PluginBuilder(logger);
            RulesPluginBuilder.ConfigureBuilder(builder, pluginDefn, language, rulesFilePath, sqaleFilePath);

            AddRoslynMetadata(tempDirectory, builder, package);
            
            builder.SetJarFilePath(fullJarPath);
            builder.Build();

            this.logger.LogInfo(UIResources.APG_PluginGenerated, fullJarPath);
        }

        private static void AddRoslynMetadata(string tempDirectory, PluginBuilder builder, IPackage package)
        {
            SourceGenerator.CreateSourceFiles(typeof(AnalyzerPluginGenerator).Assembly, RoslynResourcesRoot, tempDirectory, new Dictionary<string, string>());

            string[] sourceFiles = Directory.GetFiles(tempDirectory, "*.java", SearchOption.AllDirectories);
            Debug.Assert(sourceFiles.Any(), "Failed to correctly unpack the Roslyn analyzer specific source files");

            foreach (string sourceFile in sourceFiles)
            {
                builder.AddSourceFile(sourceFile);
            }

            builder.SetSourceCodeTokenReplacement(PackageId_Token, package.Id);
            builder.SetSourceCodeTokenReplacement(PackageVersion_Token, package.Version.ToString());
            builder.SetSourceCodeTokenReplacement(AnalyzerId_Token, package.Id);
            builder.SetSourceCodeTokenReplacement(RuleNamespace_Token, package.Id);

            foreach (string extension in Extensions)
            {
                builder.AddExtension(extension);
            }
        }
    }
}
