//-----------------------------------------------------------------------
// <copyright file="RoslynGenTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Roslyn;
using SonarQube.Plugins.Roslyn.CommandLine;
using SonarQube.Plugins.Test.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonarQube.Plugins.IntegrationTests
{
    [TestClass]
    public class RoslynGenTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void RoslynPlugin_GenerateForValidAnalyzer_Succeeds()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");

            // Create a valid analyzer package
            RoslynAnalyzer11.CSharpAnalyzer analyzer = new RoslynAnalyzer11.CSharpAnalyzer();

            string packageId = "Analyzer1.Pkgid1"; // package id is not all lowercase
            string fakeRemoteNuGetDir = TestUtils.CreateTestDirectory(this.TestContext, ".fakeRemoteNuGet");
            IPackageManager fakeRemotePkgMgr = CreatePackageManager(fakeRemoteNuGetDir);
            IPackage analyzerPkg =  AddPackage(fakeRemotePkgMgr, packageId, "1.0.2", analyzer.GetType().Assembly.Location);

            string localPackageDestination = TestUtils.CreateTestDirectory(this.TestContext, ".localpackages");

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemotePkgMgr.LocalRepository, localPackageDestination, logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            ProcessedArgs args = new ProcessedArgs(packageId, new SemanticVersion("1.0.2"), "cs", null, false, outputDir);
            bool result = apg.Generate(args);

            // Assert
            Assert.IsTrue(result);
            string jarFilePath = AssertPluginJarExists(outputDir);

            // Check the content of the files embedded in the jar
            ZipFileChecker jarChecker = new ZipFileChecker(this.TestContext, jarFilePath);


            // Check the contents of the embedded config file
            string embeddedConfigFile = jarChecker.AssertFileExists("org\\sonar\\plugins\\roslynsdk\\configuration.xml");
            RoslynSdkConfiguration config = RoslynSdkConfiguration.Load(embeddedConfigFile);

            // Check the config settings
            Assert.AreEqual("analyzer1pkgid1", config.PluginKeyDifferentiator, "Unexpected repository differentiator");
            Assert.AreEqual("roslyn.analyzer1.pkgid1.cs", config.RepositoryKey, "Unexpected repository key");
            Assert.AreEqual("cs", config.RepositoryLanguage, "Unexpected language");
            Assert.AreEqual("dummy title", config.RepositoryName, "Unexpected repository name");
            
            // Check for the expected property values required by the C# plugin
            // Property name prefixes should be lower case; the case of the value should be the same as the package id
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.cs.analyzerId", "Analyzer1.Pkgid1", config);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.cs.ruleNamespace", "Analyzer1.Pkgid1", config);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.cs.nuget.packageId", "Analyzer1.Pkgid1", config);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.cs.nuget.packageVersion", "1.0.2", config);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.cs.staticResourceName", "Analyzer1.Pkgid1.1.0.2.zip", config);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.cs.pluginKey", "analyzer1pkgid1", config);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.cs.pluginVersion", "1.0.2", config);

            // Check the contents of the manifest
            string actualManifestFilePath = jarChecker.AssertFileExists("META-INF\\MANIFEST.MF");
            string[] actualManifest = File.ReadAllLines(actualManifestFilePath);
            AssertExpectedManifestValue(WellKnownPluginProperties.Key, "analyzer1pkgid1", actualManifest);
            AssertExpectedManifestValue("Plugin-Key", "analyzer1pkgid1", actualManifest); // plugin-key should be lowercase and alphanumeric
            AssertPackagePropertiesInManifest(analyzerPkg, actualManifest);


            // Check the rules
            string actualRuleFilePath = jarChecker.AssertFileExists("." + config.RulesXmlResourcePath);
            AssertExpectedRulesExist(analyzer, actualRuleFilePath);

            // Now create another checker to check the contents of the zip file (strict check this time)
            CheckEmbeddedAnalyzerPayload(jarChecker, "static\\analyzer1.pkgid1.1.0.2.zip",
                /* zip file contents */
                "analyzers\\RoslynAnalyzer11.dll");
        }

        #region Private methods

        private IPackageManager CreatePackageManager(string rootDir)
        {
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(rootDir);
            PackageManager mgr = new PackageManager(repo, rootDir);

            return mgr;
        }

        private IPackage AddPackage(IPackageManager manager, string id, string version, string payloadAssemblyFilePath)
        {
            PackageBuilder builder = new PackageBuilder();
            builder.Id = id;
            builder.Title = "dummy title";
            builder.Version = new SemanticVersion(version);
            builder.Description = "dummy description";

            builder.Authors.Add("dummy author 1");
            builder.Authors.Add("dummy author 2");

            builder.Owners.Add("dummy owner 1");
            builder.Owners.Add("dummy owner 2");

            builder.ProjectUrl = new System.Uri("http://dummyurl/");
            builder.LicenseUrl = new Uri("http://my.license/readme.txt");


            PhysicalPackageFile file = new PhysicalPackageFile();
            file.SourcePath = payloadAssemblyFilePath;
            file.TargetPath = "analyzers/" + Path.GetFileName(payloadAssemblyFilePath);
            builder.Files.Add(file);

            using (MemoryStream stream = new MemoryStream())
            {
                builder.Save(stream);
                stream.Position = 0;

                ZipPackage pkg = new ZipPackage(stream);
                manager.InstallPackage(pkg, true, true);

                return pkg;
            }
        }

        #endregion

        #region Checks

        private static string AssertPluginJarExists(string rootDir)
        {
            string[] files = Directory.GetFiles(rootDir, "*.jar", SearchOption.TopDirectoryOnly);
            Assert.AreEqual(1, files.Length, "Expecting one and only one jar file to be created");
            return files.First();
        }
        
        private static void AssertExpectedPropertyDefinitionValue(string propertyName, string expectedValue, RoslynSdkConfiguration actualConfig)
        {
            Assert.IsNotNull(actualConfig.Properties, "Configuration Properties should not be null");

            Assert.IsTrue(actualConfig.Properties.ContainsKey(propertyName), "Expected property is not set: {0}", propertyName);

            Assert.AreEqual(expectedValue, actualConfig.Properties[propertyName], "Property does not have the expected value. Property: {0}", propertyName);
        }

        private static void AssertExpectedRulesExist(DiagnosticAnalyzer analyzer, string actualRuleFilePath)
        {
            Rules actualRules = Rules.Load(actualRuleFilePath);

            foreach (DiagnosticDescriptor descriptor in analyzer.SupportedDiagnostics)
            {
                AssertRuleExists(descriptor, actualRules);
            }
        }

        private static void AssertRuleExists(DiagnosticDescriptor descriptor, Rules rules)
        {
            IEnumerable<Rule> matches = rules.Where(r => string.Equals(r.InternalKey, descriptor.Id, System.StringComparison.Ordinal));

            Assert.AreNotEqual(0, matches.Count(), "Failed to find expected rule: {0}", descriptor.Id);
            Assert.AreEqual(1, matches.Count(), "Multiple rules have the same id: {0}", descriptor.Id);

            Rule actual = matches.Single();
            Assert.AreEqual(descriptor.Title.ToString(), actual.Name, "Unexpected rule name");
            Assert.AreEqual(descriptor.Id, actual.Key, "Unexpected rule key");

            Assert.IsNotNull(actual.Severity, "Severity should be specified");
        }

        private static void AssertPackagePropertiesInManifest(IPackage package, string[] actualManifest)
        {            
            AssertExpectedManifestValue("Plugin-Name", package.Title, actualManifest);
            AssertExpectedManifestValue("Plugin-Version", package.Version.ToString(), actualManifest);
            AssertExpectedManifestValue("Plugin-Description", package.Description, actualManifest);
            AssertExpectedManifestValue("Plugin-Organization", String.Join(",", package.Owners), actualManifest);
            AssertExpectedManifestValue("Plugin-Homepage", package.ProjectUrl.ToString(), actualManifest);
            AssertExpectedManifestValue("Plugin-Developers", String.Join(",", package.Authors), actualManifest);
            AssertExpectedManifestValue("Plugin-TermsConditionsUrl", package.LicenseUrl.ToString(), actualManifest);
        }

        private static void AssertExpectedManifestValue(string propertyName, string expectedValue, string[] actualManifest)
        {
            string expectedPrefix = propertyName + ": ";

            string match = actualManifest.SingleOrDefault(a => a.StartsWith(expectedPrefix, StringComparison.Ordinal));
            Assert.IsNotNull(match, "Failed to find expected manifest property: {0}", propertyName);

            // TODO: handle multi-line values
            string actualValue = match.Substring(expectedPrefix.Length);
            Assert.AreEqual(expectedValue, actualValue, "Unexpected manifest property value. Property: {0}", propertyName);
        }

        private void CheckEmbeddedAnalyzerPayload(ZipFileChecker jarChecker, string staticResourceName,
            params string[] expectedZipContents)
        {
            // Now create another checker to check the contents of the zip file (strict check this time)
            string embeddedZipFilePath = jarChecker.AssertFileExists(staticResourceName);

            ZipFileChecker embeddedFileChecker = new ZipFileChecker(this.TestContext, embeddedZipFilePath);
            embeddedFileChecker.AssertZipContainsOnlyExpectedFiles(expectedZipContents);
        }

        #endregion
    }
}
