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
        /// <summary>
        /// Specifies the format for the name of the placeholder SQALE file
        /// </summary>
        public const string SqaleTemplateFileNameFormat = "{0}.{1}.sqale.template.xml";
        private const string DefaultRemediationCost = "5min";

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

        public bool Generate(NuGetReference analyzerRef, string language, string sqaleFilePath, string outputDirectory)
        {
            // sqale file path is optional
            if (analyzerRef == null)
            {
                throw new ArgumentNullException("analyzeRef");
            }
            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentNullException("language");
            }
            SupportedLanguages.ThrowIfNotSupported(language);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentNullException("outputDirectory");
            }

            IPackage package = TryGetPackage(analyzerRef);
            if (package == null)
            {
                return false;
            }

            string packageDir = this.packageHandler.GetLocalPackageRootDirectory(package);
            IEnumerable<DiagnosticAnalyzer> analyzers = GetAnalyzers(packageDir, this.packageHandler.LocalCacheRoot, language);

            if (!analyzers.Any())
            {
                return false;
            }

            string createJarFilePath = null;

            string baseDirectory = CreateBaseWorkingDirectory();

            // Collect the remaining data required to build the plugin
            RoslynPluginDefinition definition = new RoslynPluginDefinition();
            definition.Language = language;
            definition.SqaleFilePath = sqaleFilePath;
            definition.PackageId = package.Id;
            definition.PackageVersion = package.Version.ToString();
            definition.Manifest = CreatePluginManifest(package);

            // Create a zip containing the required analyzer files
            definition.StaticResourceName = Path.GetFileName(packageDir) + ".zip";
            definition.SourceZipFilePath = Path.Combine(baseDirectory, definition.StaticResourceName);
            ZipFile.CreateFromDirectory(packageDir, definition.SourceZipFilePath, CompressionLevel.Optimal, false);

            definition.RulesFilePath = GenerateRulesFile(analyzers, baseDirectory);

            string generatedSqaleFile = null;
            bool generate = true;
            if (definition.SqaleFilePath == null)
            {
                generatedSqaleFile = CalculateSqaleFileName(package, outputDirectory);
                GenerateFixedSqaleFile(analyzers, generatedSqaleFile);
                Debug.Assert(File.Exists(generatedSqaleFile));
            }
            else
            {
                generate = IsValidSqaleFile(definition.SqaleFilePath);
            }

            if (generate)
            {
                createJarFilePath = BuildPlugin(definition, baseDirectory, outputDirectory);
            }

            if (generatedSqaleFile != null)
            {
                // Log a messsage about the generated sqale file at the end of the process: if we
                // log it earlier it will be too easy to miss
                this.logger.LogInfo(UIResources.APG_TemplateSqaleFileGenerated, generatedSqaleFile);
            }
            this.logger.LogInfo(UIResources.APG_PluginGenerated, createJarFilePath);


            return createJarFilePath != null;
        }

        private IPackage TryGetPackage(NuGetReference analyzerRef)
        {
            IPackage package = this.packageHandler.FetchPackage(analyzerRef.PackageId, analyzerRef.Version);
            if (package != null && PackageRequiresLicenseAcceptance(package))
            {
                // Build machines will need to install this package, it is not feasible to create plugins for packages requiring license acceptance
                this.logger.LogError(UIResources.APG_NGPackageRequiresLicenseAcceptance, package.Id, package.Version);
                package = null;
            }
            return package;
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

        /// <summary>
        /// Creates a uniquely-named temp directory for this generation run
        /// </summary>
        private string CreateBaseWorkingDirectory()
        {
            string baseDirectory = Utilities.CreateTempDirectory(".gen");
            baseDirectory = Utilities.CreateSubDirectory(baseDirectory, Guid.NewGuid().ToString());
            this.logger.LogDebug(UIResources.APG_CreatedTempWorkingDir, baseDirectory);
            return baseDirectory;
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

        private static string CalculateSqaleFileName(IPackage package, string directory)
        {
            string filePath = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                SqaleTemplateFileNameFormat,
                package.Id, package.Version.ToString());

            filePath = Path.Combine(directory, filePath);
            return filePath;
        }

        /// <summary>
        /// Generates a SQALE file with fixed remediation costs for the specified analyzers
        /// </summary>
        private void GenerateFixedSqaleFile(IEnumerable<DiagnosticAnalyzer> analyzers, string outputFilePath)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingConstantSqaleFile);

            HardcodedConstantSqaleGenerator generator = new HardcodedConstantSqaleGenerator(this.logger);

            SqaleRoot sqale = generator.GenerateSqaleData(analyzers, DefaultRemediationCost);

            Serializer.SaveModel(sqale, outputFilePath);
            this.logger.LogDebug(UIResources.APG_SqaleGeneratedToFile, outputFilePath);
        }

        /// <summary>
        /// Checks that the supplied sqale file has valid content
        /// </summary>
        private bool IsValidSqaleFile(string sqaleFilePath)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(sqaleFilePath));
            // Existence is checked when parsing the arguments
            Debug.Assert(File.Exists(sqaleFilePath), "Expecting the sqale file to exist: " + sqaleFilePath);

            try
            {
                // TODO: consider adding further checks
                Serializer.LoadModel<SqaleRoot>(sqaleFilePath);
            }
            catch(InvalidOperationException) // will be thrown for invalid xml
            {
                this.logger.LogError(UIResources.APG_InvalidSqaleFile, sqaleFilePath);
                return false;
            }
            return true;
        }


        private static PluginManifest CreatePluginManifest(IPackage package)
        {
            // The manifest properties supported by SonarQube are documented at
            // http://docs.sonarqube.org/display/DEV/Build+plugin

            PluginManifest pluginDefn = new PluginManifest();

            pluginDefn.Description = GetValidManifestString(package.Description);
            pluginDefn.Developers = GetValidManifestString(ListToString(package.Authors));

            pluginDefn.Homepage = GetValidManifestString(package.ProjectUrl?.ToString());
            pluginDefn.Key = PluginKeyUtilities.GetValidKey(package.Id);

            pluginDefn.Name = GetValidManifestString(package.Title) ?? pluginDefn.Key;
            pluginDefn.Organization = GetValidManifestString(ListToString(package.Owners));
            pluginDefn.Version = GetValidManifestString(package.Version.ToNormalizedString());

            if (package.LicenseUrl != null)
            {
                // The TermsConditionsUrl is only displayed in the "Update Center - Available" page
                // i.e. for plugins that are available through the public Update Center.
                // If the property has a value then the link will be displayed with a checkbox 
                // for acceptance.
                // It is not used when plugins are directly dropped into the extensions\plugins
                // folder of the SonarQube server.
                pluginDefn.TermsConditionsUrl = package.LicenseUrl.ToString();
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

        /// <summary>
        /// Builds the plugin and returns the name of the jar that was created
        /// </summary>
        private string BuildPlugin(RoslynPluginDefinition definition, string baseTempDirectory, string outputDirectory)
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
            return fullJarPath;
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
