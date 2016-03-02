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
using System.Linq;

namespace SonarQube.Plugins.Roslyn
{
    public class AnalyzerPluginGenerator
    {
        /// <summary>
        /// List of file extensions that should not be included in the zipped analyzer assembly
        /// </summary>
        private static readonly string[] excludedFileExtensions = { ".nupkg", ".nuspec" };

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

        public bool Generate(ProcessedArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            IPackage package = this.packageHandler.FetchPackage(args.PackageId, args.PackageVersion);
            if (package == null)
            {
                return false;
            }

            string packageDir = this.packageHandler.GetLocalPackageRootDirectory(package);
            IEnumerable<DiagnosticAnalyzer> analyzers = GetAnalyzers(packageDir, this.packageHandler.LocalCacheRoot, args.Language);
            if (!analyzers.Any())
            {
                return false;
            }

            IEnumerable<IPackage> licenseAcceptancePackages = this.GetPackagesRequiringLicenseAcceptance(package);
            if (licenseAcceptancePackages.Any() && ! args.AcceptLicenses)
            {
                this.logger.LogError(UIResources.APG_NGPackageRequiresLicenseAcceptance, package.Id, package.Version.ToString());
                this.ListPackagesRequiringLicenseAcceptance(licenseAcceptancePackages);
                return false;
            }

            string createdJarFilePath = null;

            string baseDirectory = CreateBaseWorkingDirectory();

            // Collect the remaining data required to build the plugin
            RoslynPluginDefinition definition = new RoslynPluginDefinition();
            definition.Language = args.Language;
            definition.SqaleFilePath = args.SqaleFilePath;
            definition.PackageId = package.Id;
            definition.PackageVersion = package.Version.ToString();
            definition.Manifest = CreatePluginManifest(package);

            // Create a zip containing the required analyzer files
            definition.SourceZipFilePath = this.CreateAnalyzerStaticPayloadFile(packageDir, baseDirectory);
            definition.StaticResourceName = Path.GetFileName(definition.SourceZipFilePath);

            definition.RulesFilePath = GenerateRulesFile(analyzers, baseDirectory);

            string generatedSqaleFile = null;
            bool generate = true;
            if (definition.SqaleFilePath == null)
            {
                generatedSqaleFile = CalculateSqaleFileName(package, args.OutputDirectory);
                GenerateFixedSqaleFile(analyzers, generatedSqaleFile);
                Debug.Assert(File.Exists(generatedSqaleFile));
            }
            else
            {
                generate = IsValidSqaleFile(definition.SqaleFilePath);
            }

            if (generate)
            {
                createdJarFilePath = BuildPlugin(definition, baseDirectory, args.OutputDirectory);
            }

            LogSummary(createdJarFilePath, generatedSqaleFile, licenseAcceptancePackages);

            return createdJarFilePath != null;
        }

        private void LogSummary(string createdJarFilePath, string generatedSqaleFile, IEnumerable<IPackage> licenseAcceptancePackages)
        {
            if (generatedSqaleFile != null)
            {
                // Log a messsage about the generated sqale file at the end of the process: if we
                // log it earlier it will be too easy to miss
                this.logger.LogInfo(UIResources.APG_TemplateSqaleFileGenerated, generatedSqaleFile);
            }

            if (licenseAcceptancePackages.Any())
            {
                // If we got this far then the user must have accepted
                this.logger.LogWarning(UIResources.APG_NGAcceptedPackageLicenses);
                this.ListPackagesRequiringLicenseAcceptance(licenseAcceptancePackages);
            }

            this.logger.LogInfo(UIResources.APG_PluginGenerated, createdJarFilePath);
        }

        private void ListPackagesRequiringLicenseAcceptance(IEnumerable<IPackage> licensedPackages)
        {
            foreach (IPackage package in licensedPackages)
            {
                string license;
                if (package.LicenseUrl == null)
                {
                    license = UIResources.APG_NG_UnspecifiedLicenseUrl;
                }
                else
                {
                    license = package.LicenseUrl.ToString();
                }
                this.logger.LogWarning(UIResources.APG_NG_PackageAndLicenseUrl, package.Id, package.Version, license);
            }
        }

        /// <summary>
        /// Returns all of the packages from the supplied package and its dependencies that require license acceptance
        /// </summary>
        private IEnumerable<IPackage> GetPackagesRequiringLicenseAcceptance(IPackage package)
        {
            List<IPackage> licensed = new List<IPackage>();
            if (package.RequireLicenseAcceptance)
            {
                licensed.Add(package);
            }

            licensed.AddRange(this.packageHandler.GetInstalledDependencies(package).Where(d => d.RequireLicenseAcceptance));
            return licensed;
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

        private string CreateAnalyzerStaticPayloadFile(string packageRootDir, string outputDir)
        {
            string zipFilePath = Path.GetFileName(packageRootDir) + ".zip";
            zipFilePath = Path.Combine(outputDir, zipFilePath);

            ZipExtensions.CreateFromDirectory(packageRootDir, zipFilePath, IncludeFileInZip);

            return zipFilePath;
        }

        private static bool IncludeFileInZip(string filePath)
        {
            return !excludedFileExtensions.Any(e => filePath.EndsWith(e, StringComparison.OrdinalIgnoreCase));
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

            SqaleModel sqale = generator.GenerateSqaleData(analyzers, DefaultRemediationCost);

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
                Serializer.LoadModel<SqaleModel>(sqaleFilePath);
            }
            catch(InvalidOperationException) // will be thrown for invalid xml
            {
                this.logger.LogError(UIResources.APG_InvalidSqaleFile, sqaleFilePath);
                return false;
            }
            return true;
        }

        public /* for test */ static PluginManifest CreatePluginManifest(IPackage package)
        {
            // The manifest properties supported by SonarQube are documented at
            // http://docs.sonarqube.org/display/DEV/Build+plugin

            PluginManifest pluginDefn = new PluginManifest();

            pluginDefn.Description = GetValidManifestString(package.Description);
            pluginDefn.Developers = GetValidManifestString(ListToString(package.Authors));

            pluginDefn.Homepage = GetValidManifestString(package.ProjectUrl?.ToString());
            pluginDefn.Key = PluginKeyUtilities.GetValidKey(package.Id);

            if (!String.IsNullOrWhiteSpace(package.Title))
            {
                pluginDefn.Name = GetValidManifestString(package.Title);
            }
            else
            {
                // Process the package ID to replace dot separators with spaces for use as a fallback
                pluginDefn.Name = GetValidManifestString(package.Id.Replace(".", " "));
            }

            // Fall back to using the authors if owners is empty
            string organisation;
            if (package.Owners.Any())
            {
                organisation = ListToString(package.Owners);
            }
            else
            {
                organisation = ListToString(package.Authors);
            }
            pluginDefn.Organization = GetValidManifestString(organisation);

            pluginDefn.Version = GetValidManifestString(package.Version?.ToNormalizedString());

            // The TermsConditionsUrl is only displayed in the "Update Center - Available" page
            // i.e. for plugins that are available through the public Update Center.
            // If the property has a value then the link will be displayed with a checkbox
            // for acceptance.
            // It is not used when plugins are directly dropped into the extensions\plugins
            // folder of the SonarQube server.
            pluginDefn.TermsConditionsUrl = GetValidManifestString(package.LicenseUrl?.ToString());

            // Packages from the NuGet website may have friendly short licensenames heuristically assigned, but this requires a downcast
            DataServicePackage dataServicePackage = package as DataServicePackage;
            if (!String.IsNullOrWhiteSpace(dataServicePackage?.LicenseNames))
            {
                pluginDefn.License = GetValidManifestString(dataServicePackage.LicenseNames);
            }
            else
            {
                // Fallback - use a raw URL. Not as nice-looking in the UI, but acceptable.
                pluginDefn.License = pluginDefn.TermsConditionsUrl;
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
