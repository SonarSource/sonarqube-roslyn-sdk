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
        /// The prefix expected by the C# plugin; used to identify repositories
        /// that contain Roslyn rules
        /// </summary>
        private const string RepositoryKeyPrefix = "roslyn.";

        /// <summary>
        /// List of file extensions that should not be included in the zipped analyzer assembly
        /// </summary>
        private static readonly string[] excludedFileExtensions = { ".nupkg", ".nuspec" };

        /// <summary>
        /// Specifies the format for the name of the placeholder SQALE file
        /// </summary>
        public const string SqaleTemplateFileNameFormat = "{0}.{1}.sqale.template.xml";
        private const string DefaultRemediationCost = "5min";

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

            IPackage targetPackage = this.packageHandler.FetchPackage(args.PackageId, args.PackageVersion);

            if (targetPackage == null)
            {
                return false;
            }

            IEnumerable<IPackage> dependencyPackages = this.packageHandler.GetInstalledDependencies(targetPackage);

            // Check that there are analyzers in the target package from which information can be extracted

            // Create a mapping of packages to analyzers to avoid having to search for analyzers more than once
            Dictionary<IPackage, IEnumerable<DiagnosticAnalyzer>> analyzersByPackage = new Dictionary<IPackage, IEnumerable<DiagnosticAnalyzer>>();
            IEnumerable<DiagnosticAnalyzer> targetAnalyzers = GetAnalyzers(targetPackage, args.Language);
            if (targetAnalyzers.Any())
            {
                analyzersByPackage.Add(targetPackage, targetAnalyzers);
            }
            else
            {
                this.logger.LogWarning(UIResources.APG_NoAnalyzersFound, targetPackage.Id);

                if (!args.RecurseDependencies)
                {
                    this.logger.LogWarning(UIResources.APG_NoAnalyzersInTargetSuggestRecurse);
                    return false;
                }
            }

            if (args.RecurseDependencies)
            {
                // Possible sub-case - target package has dependencies that contain analyzers
                foreach (IPackage dependencyPackage in dependencyPackages)
                {
                    IEnumerable<DiagnosticAnalyzer> dependencyAnalyzers = GetAnalyzers(dependencyPackage, args.Language);
                    if (dependencyAnalyzers.Any())
                    {
                        analyzersByPackage.Add(dependencyPackage, dependencyAnalyzers);
                    }
                    else
                    {
                        this.logger.LogWarning(UIResources.APG_NoAnalyzersFound, dependencyPackage.Id);
                    }
                }

                if (!analyzersByPackage.Any())
                {
                    return false;
                }
            }

            // Check for packages that require the user to accept a license
            IEnumerable<IPackage> licenseAcceptancePackages = this.GetPackagesRequiringLicenseAcceptance(targetPackage);
            if (licenseAcceptancePackages.Any() && !args.AcceptLicenses)
            {
                // NB: This warns for all packages under the target that require license acceptance
                // (even if they aren't related to the packages from which plugins were generated)
                this.logger.LogError(UIResources.APG_NGPackageRequiresLicenseAcceptance, targetPackage.Id, targetPackage.Version.ToString());
                this.ListPackagesRequiringLicenseAcceptance(licenseAcceptancePackages);
                return false;
            }

            List<string> generatedJarFiles = new List<string>();
            // Initial run with the user-targeted package and arguments
            if (analyzersByPackage.ContainsKey(targetPackage))
            {
                string generatedJarPath = GeneratePluginForPackage(args.OutputDirectory, args.Language, args.SqaleFilePath, args.HtmlDescriptionResourceNamespace, targetPackage, analyzersByPackage[targetPackage]);
                if (generatedJarPath == null)
                {
                    return false;
                }

                generatedJarFiles.Add(generatedJarPath);
                analyzersByPackage.Remove(targetPackage);
            }

            // Dependent package generation changes the arguments
            if (args.RecurseDependencies)
            {
                this.logger.LogWarning(UIResources.APG_RecurseEnabled_SQALENotEnabled);

                foreach (IPackage currentPackage in analyzersByPackage.Keys)
                {
                    // No way to specify the SQALE file for any but the user-targeted package at this time
                    string generatedJarPath = GeneratePluginForPackage(args.OutputDirectory, args.Language, null, args.HtmlDescriptionResourceNamespace, currentPackage, analyzersByPackage[currentPackage]);
                    if (generatedJarPath == null)
                    {
                        return false;
                    }

                    generatedJarFiles.Add(generatedJarPath);
                }
            }

            LogAcceptedPackageLicenses(licenseAcceptancePackages);

            foreach (string generatedJarFile in generatedJarFiles)
            {
                this.logger.LogInfo(UIResources.APG_PluginGenerated, generatedJarFile);
            }

            return true;
        }

        private string GeneratePluginForPackage(string outputDir, string language, string sqaleFilePath, string htmlDescriptionResourceNamespace, IPackage package, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            Debug.Assert(analyzers.Any(), "The method must be called with a populated list of DiagnosticAnalyzers.");

            this.logger.LogInfo(UIResources.APG_AnalyzersLocated, package.Id, analyzers.Count());

            string createdJarFilePath = null;

            string baseDirectory = CreateBaseWorkingDirectory();

            // Collect the remaining data required to build the plugin
            RoslynPluginDefinition definition = new RoslynPluginDefinition();
            definition.Language = language;
            definition.SqaleFilePath = sqaleFilePath;
            definition.PackageId = package.Id;
            definition.PackageVersion = package.Version.ToString();
            definition.Manifest = CreatePluginManifest(package);

            // Create a zip containing the required analyzer files
            string packageDir = this.packageHandler.GetLocalPackageRootDirectory(package);
            definition.SourceZipFilePath = this.CreateAnalyzerStaticPayloadFile(packageDir, baseDirectory);
            definition.StaticResourceName = Path.GetFileName(definition.SourceZipFilePath);

            definition.RulesFilePath = GenerateRulesFile(analyzers, htmlDescriptionResourceNamespace, baseDirectory);

            string generatedSqaleFile = null;
            bool generate = true;
            if (definition.SqaleFilePath == null)
            {
                generatedSqaleFile = CalculateSqaleFileName(package, outputDir);
                GenerateFixedSqaleFile(analyzers, generatedSqaleFile);
                Debug.Assert(File.Exists(generatedSqaleFile));
            }
            else
            {
                generate = IsValidSqaleFile(definition.SqaleFilePath);
            }

            if (generate)
            {
                createdJarFilePath = BuildPlugin(definition, outputDir);
            }

            LogMessageForGeneratedSqale(generatedSqaleFile);

            return createdJarFilePath;
        }

        private void LogMessageForGeneratedSqale(string generatedSqaleFile)
        {
            if (generatedSqaleFile != null)
            {
                // Log a message about the generated SQALE file for every plugin generated
                this.logger.LogInfo(UIResources.APG_TemplateSqaleFileGenerated, generatedSqaleFile);
            }
        }

        private void LogAcceptedPackageLicenses(IEnumerable<IPackage> licenseAcceptancePackages)
        {
            if (licenseAcceptancePackages.Any())
            {
                // If we got this far then the user must have accepted
                this.logger.LogWarning(UIResources.APG_NGAcceptedPackageLicenses);
                this.ListPackagesRequiringLicenseAcceptance(licenseAcceptancePackages);
            }
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

        /// <summary>
        /// Retrieves the analyzers contained within a given NuGet package corresponding to a given language
        /// </summary>
        private IEnumerable<DiagnosticAnalyzer> GetAnalyzers(IPackage package, string language)
        {
            string packageRootDir = this.packageHandler.GetLocalPackageRootDirectory(package);
            string additionalSearchFolder = this.packageHandler.LocalCacheRoot;

            this.logger.LogInfo(UIResources.APG_LocatingAnalyzers);
            string[] analyzerFiles = Directory.GetFiles(packageRootDir, "*.dll", SearchOption.AllDirectories);

            string roslynLanguageName = SupportedLanguages.GetRoslynLanguageName(language);
            this.logger.LogDebug(UIResources.APG_LogAnalyzerLanguage, roslynLanguageName);

            DiagnosticAssemblyScanner diagnosticAssemblyScanner = new DiagnosticAssemblyScanner(this.logger, additionalSearchFolder);
            IEnumerable<DiagnosticAnalyzer> analyzers = diagnosticAssemblyScanner.InstantiateDiagnostics(roslynLanguageName, analyzerFiles.ToArray());

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
        private string GenerateRulesFile(IEnumerable<DiagnosticAnalyzer> analyzers, string htmlDescriptionResourceNamespace, string baseDirectory)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingRules);

            Debug.Assert(analyzers.Any(), "Expecting at least one analyzer");

            string rulesFilePath = Path.Combine(baseDirectory, "rules.xml");

            RuleGenerator ruleGen = new RuleGenerator(this.logger, htmlDescriptionResourceNamespace);
            Rules rules = ruleGen.GenerateRules(analyzers);

            if (rules != null)
            {
                rules.Save(rulesFilePath, logger);
                this.logger.LogDebug(UIResources.APG_RulesGeneratedToFile, rules.Count, rulesFilePath);
            }
            else
            {
                Debug.Fail("Not expecting the generated rules to be null");
            }

            return rulesFilePath;
        }

        private static string CalculateSqaleFileName(IPackage package, string directory)
        {
            string filePath = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                SqaleTemplateFileNameFormat, package.Id, package.Version);

            filePath = Path.Combine(directory, filePath);
            return filePath;
        }

        /// <summary>
        /// Generates a SQALE file with fixed remediation costs for the specified analyzers
        /// </summary>
        private void GenerateFixedSqaleFile(IEnumerable<DiagnosticAnalyzer> analyzers, string outputFilePath)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingConstantSqaleFile);

            HardcodedConstantSqaleGenerator generator = new HardcodedConstantSqaleGenerator();

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
        private string BuildPlugin(RoslynPluginDefinition definition, string outputDirectory)
        {
            this.logger.LogInfo(UIResources.APG_GeneratingPlugin);

            // Make the .jar name match the format [artefactid]-[version].jar
            // i.e. the format expected by Maven
            Directory.CreateDirectory(outputDirectory);
            string fullJarPath = Path.Combine(outputDirectory,
                definition.Manifest.Key + "-plugin-" + definition.Manifest.Version + ".jar");

            string repositoryId = RepositoryKeyUtilities.GetValidKey(definition.PackageId + "." + definition.Language);

            string repoKey = RepositoryKeyPrefix + repositoryId;

            RoslynPluginJarBuilder builder = new RoslynPluginJarBuilder(logger);
            builder.SetLanguage(definition.Language)
                        .SetRepositoryKey(repoKey)
                        .SetRepositoryName(definition.Manifest.Name)
                        .SetRulesFilePath(definition.RulesFilePath)
                        .SetPluginManifestProperties(definition.Manifest)
                        .SetJarFilePath(fullJarPath);

            if (!string.IsNullOrWhiteSpace(definition.SqaleFilePath))
            {
                builder.SetSqaleFilePath(definition.SqaleFilePath);
            }

            AddRoslynMetadata(builder, definition, repositoryId);

            string relativeStaticFilePath = "static/" + Path.GetFileName(definition.StaticResourceName);
            builder.AddResourceFile(definition.SourceZipFilePath, relativeStaticFilePath);

            builder.Build();
            return fullJarPath;
        }

        private void AddRoslynMetadata(RoslynPluginJarBuilder builder, RoslynPluginDefinition definition, string repositoryId)
        {
            builder.SetPluginProperty(repositoryId + ".nuget.packageId", definition.PackageId);
            builder.SetPluginProperty(repositoryId + ".nuget.packageVersion", definition.PackageVersion);

            builder.SetPluginProperty(repositoryId + ".analyzerId", definition.PackageId);
            builder.SetPluginProperty(repositoryId + ".ruleNamespace", definition.PackageId);
            builder.SetPluginProperty(repositoryId + ".staticResourceName", definition.StaticResourceName);
            builder.SetPluginProperty(repositoryId + ".pluginKey", definition.Manifest.Key);
            builder.SetPluginProperty(repositoryId + ".pluginVersion", definition.Manifest.Version);
        }
    }
}
