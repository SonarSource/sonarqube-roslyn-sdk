//-----------------------------------------------------------------------
// <copyright file="AnalyzerPluginGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet;
using SonarQube.Plugins.Common;
using SonarQube.Plugins.Roslyn.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SonarQube.Plugins.Roslyn
{
    public class AnalyzerPluginGenerator
    {
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

        private readonly INuGetPackageHandler packageHandler;
        private readonly SonarQube.Plugins.Common.ILogger logger;

        public AnalyzerPluginGenerator(INuGetPackageHandler packageHandler, SonarQube.Plugins.Common.ILogger logger)
        {
            if (packageHandler == null)
            {
                throw new ArgumentNullException("packageHandler");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.packageHandler = packageHandler;
            this.logger = logger;
        }

        public bool Generate(NuGetReference analyzeRef, string language, string sqaleFilePath, string outputDirectory)
        {
            // sqale file path is optional
            if (analyzeRef == null)
            {
                throw new ArgumentNullException("analyzeRef");
            }
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentNullException("language");
            }
            SupportedLanguages.ThrowIfNotSupported(language);

            string nuGetDirectory = Utilities.CreateTempDirectory(".nuget");
            IPackage package = this.packageHandler.FetchPackage(analyzeRef.PackageId, analyzeRef.Version, nuGetDirectory);

            if (package != null)
            {
                // Create a uniquely-named temp directory for this generation run
                string baseDirectory = Utilities.CreateTempDirectory(".gen");
                baseDirectory = CreateSubDirectory(baseDirectory, Guid.NewGuid().ToString());
                this.logger.LogDebug(UIResources.APG_CreatedTempWorkingDir, baseDirectory);

                PluginManifest pluginDefn = CreatePluginDefinition(package);

                string rulesFilePath = Path.Combine(baseDirectory, "rules.xml");

                bool success = TryGenerateRulesFile(package, nuGetDirectory, baseDirectory, rulesFilePath, language);

                if (success)
                {
                    BuildPlugin(package, language, rulesFilePath, sqaleFilePath, pluginDefn, baseDirectory, outputDirectory);
                }
            }

            return package != null;
        }

        private static string CreateSubDirectory(string parent, string child)
        {
            string newDir = Path.Combine(parent, child);
            Directory.CreateDirectory(newDir);
            return newDir;
        }

        /// <summary>
        /// Attempts to generate a rules file for assemblies in the specified package.
        /// </summary>
        /// <param name="additionalSearchFolder">Root directory to search when looking for analyzer dependencies</param>
        /// <param name="baseTempDir">Base temporary working directory for this generation run</param>
        /// <param name="rulesFilePath">Full name of the file to create</param>
        private bool TryGenerateRulesFile(IPackage package, string additionalSearchFolder, string baseTempDir, string rulesFilePath, string language)
        {
            bool success = false;
            this.logger.LogInfo(UIResources.APG_GeneratingRules);

            this.logger.LogInfo(UIResources.APG_LocatingAnalyzers);

            string[] files = GetFilesFromPackage(package, baseTempDir).ToArray();

            string roslynLanguageName = SupportedLanguages.GetRoslynLanguageName(language);
            this.logger.LogDebug(UIResources.APG_LogAnalyzerLanguage, roslynLanguageName);

            DiagnosticAssemblyScanner diagnosticAssemblyScanner = new DiagnosticAssemblyScanner(this.logger, additionalSearchFolder);
            IEnumerable<DiagnosticAnalyzer> analyzers = diagnosticAssemblyScanner.InstantiateDiagnostics(roslynLanguageName, files.ToArray());

            this.logger.LogInfo(UIResources.APG_AnalyzersLocated, analyzers.Count());

            if (analyzers.Any())
            {
                RuleGenerator ruleGen = new RuleGenerator(this.logger);
                Rules rules = ruleGen.GenerateRules(analyzers);

                Debug.Assert(rules != null, "Not expecting the generated rules to be null");

                if (rules != null)
                {
                    rules.Save(rulesFilePath, logger);
                    this.logger.LogDebug(UIResources.APG_RulesGeneratedToFile, rules.Count, rulesFilePath);
                    success = true;
                }
            }
            else
            {
                this.logger.LogWarning(UIResources.APG_NoAnalyzersFound);
            }
            return success;
        }

        private IEnumerable<string> GetFilesFromPackage(IPackage package, string baseTempDir)
        {
            // We can't directly get the paths to the files in package so
            // we have to extract them first
            string extractDir = CreateSubDirectory(baseTempDir, ".extract");
            this.logger.LogDebug(UIResources.APG_ExtractingPackageFiles, extractDir);
            PhysicalFileSystem fileSystem = new PhysicalFileSystem(extractDir);
            package.ExtractContents(fileSystem, ".");

            string[] files = Directory.GetFiles(extractDir, "*.*", SearchOption.AllDirectories);
            return files;
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

        private void BuildPlugin(IPackage package, string language, string rulesFilePath, string sqaleFilePath, PluginManifest pluginDefn, string baseTempDirectory, string outputDirectory)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingPlugin);

            // Make the .jar name match the format [artefactid]-[version].jar
            // i.e. the format expected by Maven
            Directory.CreateDirectory(outputDirectory);
            string fullJarPath = Path.Combine(outputDirectory,
                package.Id + "-plugin-" + pluginDefn.Version + ".jar");

            PluginBuilder builder = new PluginBuilder(logger);
            RulesPluginBuilder.ConfigureBuilder(builder, pluginDefn, language, rulesFilePath, sqaleFilePath);

            AddRoslynMetadata(baseTempDirectory, builder, package);
            
            builder.SetJarFilePath(fullJarPath);
            builder.Build();

            this.logger.LogInfo(UIResources.APG_PluginGenerated, fullJarPath);
        }

        private void AddRoslynMetadata(string baseTempDirectory, PluginBuilder builder, IPackage package)
        {
            string sourcesDir = CreateSubDirectory(baseTempDirectory, "src");
            this.logger.LogDebug(UIResources.APG_CreatingRoslynSources, sourcesDir);

            SourceGenerator.CreateSourceFiles(typeof(AnalyzerPluginGenerator).Assembly, RoslynResourcesRoot, sourcesDir, new Dictionary<string, string>());

            string[] sourceFiles = Directory.GetFiles(sourcesDir, "*.java", SearchOption.AllDirectories);
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
