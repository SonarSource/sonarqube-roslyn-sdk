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
            ProcessedArgs args = new ProcessedArgs(packageId, new SemanticVersion("1.0.2"), "cs", null, false, false, outputDir);
            bool result = apg.Generate(args);

            // Assert
            Assert.IsTrue(result);
            string jarFilePath = AssertPluginJarsExist(outputDir, 1).First();

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

        // Test verifies https://jira.sonarsource.com/browse/SFSRAP-32
        [TestMethod]
        public void RoslynPlugin_GenerateForDependencyAnalyzers_Succeeds()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, ".out");
            string dummyContentFile = TestUtils.CreateTextFile("dummy.txt", outputDir, "non-analyzer content file");

            // Create a valid analyzer package
            RoslynAnalyzer11.CSharpAnalyzer analyzer = new RoslynAnalyzer11.CSharpAnalyzer();

            string fakeRemoteNuGetDir = TestUtils.CreateTestDirectory(this.TestContext, ".fakeRemoteNuGet");
            IPackageManager fakeRemotePkgMgr = CreatePackageManager(fakeRemoteNuGetDir);
            IPackage child1 = AddPackage(fakeRemotePkgMgr, "Analyzer.Child1", "1.1.0", analyzer.GetType().Assembly.Location);
            IPackage child2 = AddPackage(fakeRemotePkgMgr, "Analyzer.Child2", "1.2.0", analyzer.GetType().Assembly.Location);
            IPackage targetPkg = AddPackage(fakeRemotePkgMgr, "Empty.Parent", "1.0.0", dummyContentFile, child1, child2);

            string localPackageDestination = TestUtils.CreateTestDirectory(this.TestContext, ".localpackages");

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemotePkgMgr.LocalRepository, localPackageDestination, logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            ProcessedArgs args = new ProcessedArgs(targetPkg.Id, targetPkg.Version, "cs", null, false, 
                true /* generate plugins for dependencies with analyzers*/, outputDir);
            bool result = apg.Generate(args);

            // Assert
            Assert.IsTrue(result);
            string[] jarFilePaths = AssertPluginJarsExist(outputDir, 2); // Expecting one plugin per dependency with analyzers

            foreach (string jarFilePath in jarFilePaths)
            {
                // Check the content of the files embedded in the jar
                ZipFileChecker jarChecker = new ZipFileChecker(this.TestContext, jarFilePath);

                // Check the contents of the embedded config file
                string embeddedConfigFile = jarChecker.AssertFileExists("org\\sonar\\plugins\\roslynsdk\\configuration.xml");
                RoslynSdkConfiguration config = RoslynSdkConfiguration.Load(embeddedConfigFile);

                // Check the config settings, find which of the dependencies the jar is for (so that we can check the correct strings)
                IPackage originalPkg = null;
                if (config.PluginKeyDifferentiator.Equals("analyzerchild1"))
                {
                    originalPkg = child1;
                }
                if (config.PluginKeyDifferentiator.Equals("analyzerchild2"))
                {
                    originalPkg = child2;
                }
                Assert.IsNotNull(originalPkg, "Unexpected repository differentiator");

                string pluginId = originalPkg.Id.ToLower();
                Assert.AreEqual("roslyn." + pluginId + ".cs", config.RepositoryKey, "Unexpected repository key");
                Assert.AreEqual("cs", config.RepositoryLanguage, "Unexpected language");
                Assert.AreEqual("dummy title", config.RepositoryName, "Unexpected repository name");

                // Check for the expected property values required by the C# plugin
                // Property name prefixes should be lower case; the case of the value should be the same as the package id
                AssertExpectedPropertyDefinitionValue(pluginId + ".cs.analyzerId", originalPkg.Id, config);
                AssertExpectedPropertyDefinitionValue(pluginId + ".cs.ruleNamespace", originalPkg.Id, config);
                AssertExpectedPropertyDefinitionValue(pluginId + ".cs.nuget.packageId", originalPkg.Id, config);
                AssertExpectedPropertyDefinitionValue(pluginId + ".cs.nuget.packageVersion", originalPkg.Version.ToString(), config);
                AssertExpectedPropertyDefinitionValue(pluginId + ".cs.staticResourceName", originalPkg.Id + "." + originalPkg.Version + ".zip", config);
                AssertExpectedPropertyDefinitionValue(pluginId + ".cs.pluginKey", pluginId.Replace(".", ""), config);
                AssertExpectedPropertyDefinitionValue(pluginId + ".cs.pluginVersion", originalPkg.Version.ToString(), config);

                // Check the contents of the manifest
                string actualManifestFilePath = jarChecker.AssertFileExists("META-INF\\MANIFEST.MF");
                string[] actualManifest = File.ReadAllLines(actualManifestFilePath);
                AssertExpectedManifestValue(WellKnownPluginProperties.Key, pluginId.Replace(".", ""), actualManifest);
                AssertExpectedManifestValue("Plugin-Key", pluginId.Replace(".", ""), actualManifest); // plugin-key should be lowercase and alphanumeric
                AssertPackagePropertiesInManifest(originalPkg, actualManifest);


                // Check the rules
                string actualRuleFilePath = jarChecker.AssertFileExists("." + config.RulesXmlResourcePath);
                AssertExpectedRulesExist(analyzer, actualRuleFilePath);

                // Now create another checker to check the contents of the zip file (strict check this time)
                CheckEmbeddedAnalyzerPayload(jarChecker, "static\\" + pluginId + "." + originalPkg.Version + ".zip",
                    /* zip file contents */
                    "analyzers\\RoslynAnalyzer11.dll");
            }
        }

        #region Private methods

        private IPackageManager CreatePackageManager(string rootDir)
        {
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository(rootDir);
            PackageManager mgr = new PackageManager(repo, rootDir);

            return mgr;
        }

        private IPackage AddPackage(IPackageManager manager, string id, string version, string payloadAssemblyFilePath, params IPackage[] dependencies)
        {
            PackageBuilder builder = new PackageBuilder();

            ManifestMetadata metadata = new ManifestMetadata()
            {
                Authors = "dummy author 1,dummy author 2",
                Owners = "dummy owner 1,dummy owner 2",
                Title = "dummy title",
                Version = new SemanticVersion(version).ToString(),
                Id = id,
                Description = "dummy description",
                LicenseUrl = "http://my.license/readme.txt",
                ProjectUrl = "http://dummyurl/"
            };

            List<ManifestDependency> dependencyList = new List<ManifestDependency>();
            foreach (IPackage dependencyNode in dependencies)
            {
                dependencyList.Add(new ManifestDependency()
                {
                    Id = dependencyNode.Id,
                    Version = dependencyNode.Version.ToString(),
                });
            }

            List<ManifestDependencySet> dependencySetList = new List<ManifestDependencySet>()
            {
                new ManifestDependencySet()
                {
                    Dependencies = dependencyList
                }
            };
            metadata.DependencySets = dependencySetList;

            builder.Populate(metadata);

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

        private static string[] AssertPluginJarsExist(string rootDir, int numberOfJars)
        {
            string[] files = Directory.GetFiles(rootDir, "*.jar", SearchOption.TopDirectoryOnly);
            Assert.AreEqual(numberOfJars, files.Length, "Expecting only " + numberOfJars + "jar file to be created");
            return files;
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
