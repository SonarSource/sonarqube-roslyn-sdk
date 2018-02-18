/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Roslyn;
using SonarQube.Plugins.Roslyn.CommandLine;
using SonarQube.Plugins.Test.Common;

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
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");

            // Create a valid analyzer package
            RoslynAnalyzer11.CSharpAnalyzer analyzer = new RoslynAnalyzer11.CSharpAnalyzer();

            string packageId = "Analyzer1.Pkgid1"; // package id is not all lowercase
            string fakeRemoteNuGetDir = TestUtils.CreateTestDirectory(TestContext, ".fakeRemoteNuGet");
            IPackageManager fakeRemotePkgMgr = CreatePackageManager(fakeRemoteNuGetDir);
            IPackage analyzerPkg =  AddPackage(fakeRemotePkgMgr, packageId, "1.0.2", analyzer.GetType().Assembly.Location);

            string localPackageDestination = TestUtils.CreateTestDirectory(TestContext, ".localpackages");

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemotePkgMgr.LocalRepository, localPackageDestination, logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            ProcessedArgs args = new ProcessedArgs(packageId, new SemanticVersion("1.0.2"), "cs", null, null, false, false, outputDir);
            bool result = apg.Generate(args);

            // Assert
            result.Should().BeTrue();

            // Expecting one plugin per dependency with analyzers
            CheckJarGeneratedForPackage(outputDir, analyzer, analyzerPkg);
            AssertJarsGenerated(outputDir, 1);
        }

        // Test verifies https://jira.sonarsource.com/browse/SFSRAP-32
        [TestMethod]
        public void RoslynPlugin_GenerateForDependencyAnalyzers_Succeeds()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
            string dummyContentFile = TestUtils.CreateTextFile("dummy.txt", outputDir, "non-analyzer content file");

            // Create a valid analyzer package
            RoslynAnalyzer11.CSharpAnalyzer analyzer = new RoslynAnalyzer11.CSharpAnalyzer();

            string fakeRemoteNuGetDir = TestUtils.CreateTestDirectory(TestContext, ".fakeRemoteNuGet");
            IPackageManager fakeRemotePkgMgr = CreatePackageManager(fakeRemoteNuGetDir);
            IPackage child1 = AddPackage(fakeRemotePkgMgr, "Analyzer.Child1", "1.1.0", analyzer.GetType().Assembly.Location);
            IPackage child2 = AddPackage(fakeRemotePkgMgr, "Analyzer.Child2", "1.2.0", analyzer.GetType().Assembly.Location);
            IPackage targetPkg = AddPackage(fakeRemotePkgMgr, "Empty.Parent", "1.0.0", dummyContentFile, child1, child2);

            string localPackageDestination = TestUtils.CreateTestDirectory(TestContext, ".localpackages");

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemotePkgMgr.LocalRepository, localPackageDestination, logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            ProcessedArgs args = new ProcessedArgs(targetPkg.Id, targetPkg.Version, "cs", null, null, false, 
                true /* generate plugins for dependencies with analyzers*/, outputDir);
            bool result = apg.Generate(args);

            // Assert
            result.Should().BeTrue();

            // Expecting one plugin per dependency with analyzers
            CheckJarGeneratedForPackage(outputDir, analyzer, child1);
            CheckJarGeneratedForPackage(outputDir, analyzer, child2);
            AssertJarsGenerated(outputDir, 2);
        }

        [TestMethod]
        public void RoslynPlugin_GenerateForMultiLevelAnalyzers_Succeeds()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");

            // Create a valid analyzer package
            RoslynAnalyzer11.CSharpAnalyzer analyzer = new RoslynAnalyzer11.CSharpAnalyzer();

            // Parent and children all have analyzers, expecting plugins for all three
            string fakeRemoteNuGetDir = TestUtils.CreateTestDirectory(TestContext, ".fakeRemoteNuGet");
            IPackageManager fakeRemotePkgMgr = CreatePackageManager(fakeRemoteNuGetDir);
            IPackage child1 = AddPackage(fakeRemotePkgMgr, "Analyzer.Child1", "1.1.0", analyzer.GetType().Assembly.Location);
            IPackage child2 = AddPackage(fakeRemotePkgMgr, "Analyzer.Child2", "1.2.0", analyzer.GetType().Assembly.Location);
            IPackage targetPkg = AddPackage(fakeRemotePkgMgr, "Empty.Parent", "1.0.0", analyzer.GetType().Assembly.Location, child1, child2);

            string localPackageDestination = TestUtils.CreateTestDirectory(TestContext, ".localpackages");

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemotePkgMgr.LocalRepository, localPackageDestination, logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            ProcessedArgs args = new ProcessedArgs(targetPkg.Id, targetPkg.Version, "cs", null, null, false,
                true /* generate plugins for dependencies with analyzers*/, outputDir);
            bool result = apg.Generate(args);

            // Assert
            result.Should().BeTrue();

            // Expecting one plugin per dependency with analyzers
            CheckJarGeneratedForPackage(outputDir, analyzer, targetPkg);
            CheckJarGeneratedForPackage(outputDir, analyzer, child1);
            CheckJarGeneratedForPackage(outputDir, analyzer, child2);
            AssertJarsGenerated(outputDir, 3);
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

            PhysicalPackageFile file = new PhysicalPackageFile
            {
                SourcePath = payloadAssemblyFilePath,
                TargetPath = "analyzers/" + Path.GetFileName(payloadAssemblyFilePath)
            };
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

        #endregion Private methods

        #region Checks

        private static string[] GetGeneratedJars(string rootDir)
        {
            return Directory.GetFiles(rootDir, "*.jar", SearchOption.TopDirectoryOnly);
        }

        private void CheckJarGeneratedForPackage(string rootDir, DiagnosticAnalyzer analyzer, IPackage package)
        {
            string jarFilePath = GetGeneratedJars(rootDir).SingleOrDefault(r => r.Contains(package.Id.Replace(".", "").ToLower()));
            jarFilePath.Should().NotBeNull();

            // Check the content of the files embedded in the jar
            ZipFileChecker jarChecker = new ZipFileChecker(TestContext, jarFilePath);

            // Check the contents of the embedded config file
            string embeddedConfigFile = jarChecker.AssertFileExists("org\\sonar\\plugins\\roslynsdk\\configuration.xml");
            RoslynSdkConfiguration config = RoslynSdkConfiguration.Load(embeddedConfigFile);

            // Check the config settings
            package.Should().NotBeNull("Unexpected repository differentiator");

            string pluginId = package.Id.ToLower();
            config.RepositoryKey.Should().Be("roslyn." + pluginId + ".cs", "Unexpected repository key");
            config.RepositoryLanguage.Should().Be("cs", "Unexpected language");
            config.RepositoryName.Should().Be("dummy title", "Unexpected repository name");

            // Check for the expected property values required by the C# plugin
            // Property name prefixes should be lower case; the case of the value should be the same as the package id
            AssertExpectedPropertyDefinitionValue(pluginId + ".cs.analyzerId", package.Id, config);
            AssertExpectedPropertyDefinitionValue(pluginId + ".cs.ruleNamespace", package.Id, config);
            AssertExpectedPropertyDefinitionValue(pluginId + ".cs.nuget.packageId", package.Id, config);
            AssertExpectedPropertyDefinitionValue(pluginId + ".cs.nuget.packageVersion", package.Version.ToString(), config);
            AssertExpectedPropertyDefinitionValue(pluginId + ".cs.staticResourceName", package.Id + "." + package.Version + ".zip", config);
            AssertExpectedPropertyDefinitionValue(pluginId + ".cs.pluginKey", pluginId.Replace(".", ""), config);
            AssertExpectedPropertyDefinitionValue(pluginId + ".cs.pluginVersion", package.Version.ToString(), config);

            // Check the contents of the manifest
            string actualManifestFilePath = jarChecker.AssertFileExists("META-INF\\MANIFEST.MF");
            string[] actualManifest = File.ReadAllLines(actualManifestFilePath);
            AssertExpectedManifestValue(WellKnownPluginProperties.Key, pluginId.Replace(".", ""), actualManifest);
            AssertExpectedManifestValue("Plugin-Key", pluginId.Replace(".", ""), actualManifest); // plugin-key should be lowercase and alphanumeric
            AssertPackagePropertiesInManifest(package, actualManifest);

            // Check the rules
            string actualRuleFilePath = jarChecker.AssertFileExists("." + config.RulesXmlResourcePath);
            AssertExpectedRulesExist(analyzer, actualRuleFilePath);

            // Now create another checker to check the contents of the zip file (strict check this time)
            CheckEmbeddedAnalyzerPayload(jarChecker, "static\\" + pluginId + "." + package.Version + ".zip",
                /* zip file contents */
                "analyzers\\RoslynAnalyzer11.dll");
        }

        private static void AssertJarsGenerated(string rootDir, int expectedCount)
        {
            string[] files = Directory.GetFiles(rootDir, "*.jar", SearchOption.TopDirectoryOnly);
            files.Length.Should().Be(expectedCount, "Unexpected number of JAR files generated");
        }

        private static void AssertExpectedPropertyDefinitionValue(string propertyName, string expectedValue, RoslynSdkConfiguration actualConfig)
        {
            actualConfig.Properties.Should().NotBeNull("Configuration Properties should not be null");

            actualConfig.Properties.ContainsKey(propertyName).Should().BeTrue("Expected property is not set: {0}", propertyName);

            actualConfig.Properties[propertyName].Should().Be(expectedValue, "Property does not have the expected value. Property: {0}", propertyName);
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

            matches.Count().Should().Be(1, "Multiple rules have the same id: {0}", descriptor.Id);

            Rule actual = matches.Single();
            actual.Name.Should().Be(descriptor.Title.ToString(), "Unexpected rule name");
            actual.Key.Should().Be(descriptor.Id, "Unexpected rule key");

            actual.Severity.Should().NotBeNull("Severity should be specified");
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
            match.Should().NotBeNull("Failed to find expected manifest property: {0}", propertyName);

            // TODO: handle multi-line values
            string actualValue = match.Substring(expectedPrefix.Length);
            actualValue.Should().Be(expectedValue, "Unexpected manifest property value. Property: {0}", propertyName);
        }

        private void CheckEmbeddedAnalyzerPayload(ZipFileChecker jarChecker, string staticResourceName,
            params string[] expectedZipContents)
        {
            // Now create another checker to check the contents of the zip file (strict check this time)
            string embeddedZipFilePath = jarChecker.AssertFileExists(staticResourceName);

            ZipFileChecker embeddedFileChecker = new ZipFileChecker(TestContext, embeddedZipFilePath);
            embeddedFileChecker.AssertZipContainsOnlyExpectedFiles(expectedZipContents);
        }

        #endregion Checks
    }
}