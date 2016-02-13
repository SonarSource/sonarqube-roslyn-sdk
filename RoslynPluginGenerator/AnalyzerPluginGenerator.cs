//-----------------------------------------------------------------------
// <copyright file="AnalyzerPluginGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet;
using SonarLint.XmlDescriptor;
using SonarQube.Plugins.Common;
using SonarQube.Plugins.Roslyn.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            "RoslynProperties.NuGetPackageVersion",
            "RoslynProperties.AnalyzerResourceName",
            "RoslynProperties.PluginKey",
            "RoslynProperties.PluginVersion"
        };

        private const string AnalyzerId_Token = "[ROSLYN_ANALYZER_ID]";
        private const string RuleNamespace_Token = "[ROSLYN_RULE_NAMESPACE]";
        private const string PackageId_Token = "[ROSLYN_NUGET_PACKAGE_ID]";
        private const string PackageVersion_Token = "[ROSLYN_NUGET_PACKAGE_VERSION]";
        private const string StaticResourceName_Token = "[ROSLYN_STATIC_RESOURCENAME]";
        private const string PluginKey_Token = "[ROSLYN_PLUGIN_KEY]";
        private const string PluginVersion_Token = "[ROSLYN_PLUGIN_VERSION]";

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

            IPackage package = this.packageHandler.FetchPackage(analyzeRef.PackageId, analyzeRef.Version);

            if (package != null)
            {
                // Build machines will need to install this package, it is not feasible to create plugins for packages requiring license acceptance
                if (PackageRequiresLicenseAcceptance(package))
                {
                    this.logger.LogError(UIResources.APG_NGPackageRequiresLicenseAcceptance, package.Id, package.Version);
                    return false;
                }

                // Create a uniquely-named temp directory for this generation run
                string baseDirectory = Utilities.CreateTempDirectory(".gen");
                baseDirectory = Utilities.CreateSubDirectory(baseDirectory, Guid.NewGuid().ToString());
                this.logger.LogDebug(UIResources.APG_CreatedTempWorkingDir, baseDirectory);

                // Collect the remaining data required to build the plugin
                RoslynPluginDefinition definition = new RoslynPluginDefinition();
                definition.Language = language;
                definition.SqaleFilePath = sqaleFilePath;
                definition.PackageId = package.Id;
                definition.PackageVersion = package.Version.ToString();
                definition.Manifest = CreatePluginDefinition(package);

                string packageDir = this.packageHandler.GetLocalPackageRootDirectory(package);

                definition.StaticResourceName = Path.GetFileName(packageDir) + ".zip";
                definition.SourceZipFilePath = Path.Combine(baseDirectory, definition.StaticResourceName);
                ZipFile.CreateFromDirectory(packageDir, definition.SourceZipFilePath, CompressionLevel.Optimal, false);

                IEnumerable<DiagnosticAnalyzer> analyzers = GetAnalyzers(packageDir, this.packageHandler.LocalCacheRoot, language);

                if (analyzers.Any())
                {
                    definition.RulesFilePath = GenerateRulesFile(analyzers, baseDirectory);

                    if (definition.SqaleFilePath == null)
                    {
                        definition.SqaleFilePath = GenerateFixedSqaleFile(analyzers, baseDirectory);
                    }

                    BuildPlugin(definition, baseDirectory, outputDirectory);
                }
            }

            return package != null;
        }

        /// <summary>
        /// Recursively checks a package and all dependencies for the presence of the RequireLicenseAcceptance flag.
        /// </summary>
        /// <param name="package">The package to test.</param>
        /// <returns>Returns true if any of the packages in the dependency tree require license acceptance.</returns>
        private bool PackageRequiresLicenseAcceptance(IPackage package)
        {
            if (package.RequireLicenseAcceptance)
            {
                return true;
            }
            foreach (PackageDependencySet dependencySet in package.DependencySets)
            {
                foreach (PackageDependency dependency in dependencySet.Dependencies)
                {
                    // Also check that no dependencies require license acceptance
                    IPackage dependencyPkg = packageHandler.FetchPackage(dependency.Id, dependency.VersionSpec.MaxVersion);
                    if (PackageRequiresLicenseAcceptance(dependencyPkg))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private IEnumerable<DiagnosticAnalyzer> GetAnalyzers(string packageRootDir, string additionalSearchFolder, string language)
        {
            this.logger.LogInfo(UIResources.APG_LocatingAnalyzers);
            string[] analyzerFiles = Directory.GetFiles(packageRootDir, "*.dll", SearchOption.AllDirectories);

            string roslynLanguageName = SupportedLanguages.GetRoslynLanguageName(language);
            this.logger.LogDebug(UIResources.APG_LogAnalyzerLanguage, roslynLanguageName);

            DiagnosticAssemblyScanner diagnosticAssemblyScanner = new DiagnosticAssemblyScanner(this.logger, additionalSearchFolder);
            IEnumerable<DiagnosticAnalyzer> analyzers = diagnosticAssemblyScanner.InstantiateDiagnostics(roslynLanguageName, analyzerFiles.ToArray());

            if (analyzers.Any())
            {
                this.logger.LogInfo(UIResources.APG_AnalyzersLocated, analyzers.Count());
            }
            else
            {
                this.logger.LogWarning(UIResources.APG_NoAnalyzersFound);
            }
            return analyzers;
        }

        /// <summary>
        /// Generate a rules file for the specified analyzers
        /// </summary>
        /// <returns>The full path to the generated file</returns>
        private string GenerateRulesFile(IEnumerable<DiagnosticAnalyzer> analyzers, string baseDirectory)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingRules);

            Debug.Assert(analyzers.Any(), "Expecting at least one analyzer");

            string rulesFilePath = Path.Combine(baseDirectory, "rules.xml");

            RuleGenerator ruleGen = new RuleGenerator(this.logger);
            Rules rules = ruleGen.GenerateRules(analyzers);

            Debug.Assert(rules != null, "Not expecting the generated rules to be null");

            if (rules != null)
            {
                rules.Save(rulesFilePath, logger);
                this.logger.LogDebug(UIResources.APG_RulesGeneratedToFile, rules.Count, rulesFilePath);
            }

            return rulesFilePath;
        }

        /// <summary>
        /// Generates a SQALE file with fixed remediation costs for the specified analyzers
        /// </summary>
        /// <returns>The full path to the generated file</returns>
        private string GenerateFixedSqaleFile(IEnumerable<DiagnosticAnalyzer> analyzers, string baseDirectory)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingConstantSqaleFile);

            HardcodedConstantSqaleGenerator generator = new HardcodedConstantSqaleGenerator(this.logger);

            string sqaleFilePath = Path.Combine(baseDirectory, "sqale.xml");

            SqaleRoot sqale = generator.GenerateSqaleData(analyzers, "15min");

            Serializer.SaveModel(sqale, sqaleFilePath);
            this.logger.LogDebug(UIResources.APG_SqaleGeneratedToFile, sqaleFilePath);

            return sqaleFilePath;
        }

        private static PluginManifest CreatePluginDefinition(IPackage package)
        {
            PluginManifest pluginDefn = new PluginManifest();

            pluginDefn.Description = GetValidManifestString(package.Description);
            pluginDefn.Developers = GetValidManifestString(ListToString(package.Authors));

            pluginDefn.Homepage = GetValidManifestString(package.ProjectUrl?.ToString());
            pluginDefn.Key = PluginKeyUtilities.GetValidKey(package.Id);

            pluginDefn.Name = GetValidManifestString(package.Title) ?? pluginDefn.Key;
            pluginDefn.Organization = GetValidManifestString(ListToString(package.Owners));
            pluginDefn.Version = GetValidManifestString(package.Version.ToNormalizedString());

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

        private void BuildPlugin(RoslynPluginDefinition definition, string baseTempDirectory, string outputDirectory)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingPlugin);

            // Make the .jar name match the format [artefactid]-[version].jar
            // i.e. the format expected by Maven
            Directory.CreateDirectory(outputDirectory);
            string fullJarPath = Path.Combine(outputDirectory,
                definition.Manifest.Key + "-plugin-" + definition.Manifest.Version + ".jar");

            string repoKey = RepositoryKeyUtilities.GetValidKey(definition.PackageId + "." + definition.Language);

            RulesPluginBuilder builder = new RulesPluginBuilder(logger);
            builder.SetLanguage(definition.Language)
                        .SetRepositoryKey(repoKey)
                        .SetRulesFilePath(definition.RulesFilePath)
                        .SetProperties(definition.Manifest)
                        .SetJarFilePath(fullJarPath);

            if (!string.IsNullOrWhiteSpace(definition.SqaleFilePath))
            {
                builder.SetSqaleFilePath(definition.SqaleFilePath);
            }

            AddRoslynMetadata(baseTempDirectory, builder, definition);

            string relativeStaticFilePath = "static/" + Path.GetFileName(definition.StaticResourceName);
            builder.AddResourceFile(definition.SourceZipFilePath, relativeStaticFilePath);

            builder.Build();

            this.logger.LogInfo(UIResources.APG_PluginGenerated, fullJarPath);
        }

        private void AddRoslynMetadata(string baseTempDirectory, PluginBuilder builder, RoslynPluginDefinition definition)
        {
            string sourcesDir = Utilities.CreateSubDirectory(baseTempDirectory, "src");
            this.logger.LogDebug(UIResources.APG_CreatingRoslynSources, sourcesDir);

            SourceGenerator.CreateSourceFiles(typeof(AnalyzerPluginGenerator).Assembly, RoslynResourcesRoot, sourcesDir, new Dictionary<string, string>());

            string[] sourceFiles = Directory.GetFiles(sourcesDir, "*.java", SearchOption.AllDirectories);
            Debug.Assert(sourceFiles.Any(), "Failed to correctly unpack the Roslyn analyzer specific source files");

            foreach (string sourceFile in sourceFiles)
            {
                builder.AddSourceFile(sourceFile);
            }

            builder.SetSourceCodeTokenReplacement(PackageId_Token, definition.PackageId);
            builder.SetSourceCodeTokenReplacement(PackageVersion_Token, definition.PackageVersion);
            builder.SetSourceCodeTokenReplacement(AnalyzerId_Token, definition.PackageId);
            builder.SetSourceCodeTokenReplacement(RuleNamespace_Token, definition.PackageId);
            builder.SetSourceCodeTokenReplacement(StaticResourceName_Token, definition.StaticResourceName);
            builder.SetSourceCodeTokenReplacement(PluginKey_Token, definition.Manifest.Key);
            builder.SetSourceCodeTokenReplacement(PluginVersion_Token, definition.Manifest.Version);

            foreach (string extension in Extensions)
            {
                builder.AddExtension(extension);
            }
        }
    }
}
