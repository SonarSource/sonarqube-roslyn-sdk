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
            PluginInspector inspector = CreatePluginInspector(logger);

            // Create a valid analyzer package
            ExampleAnalyzer1.CSharpAnalyzer analyzer = new ExampleAnalyzer1.CSharpAnalyzer();

            string packageId = "Analyzer1.Pkgid1"; // package id is not all lowercase
            string fakeRemoteNuGetDir = TestUtils.CreateTestDirectory(this.TestContext, ".fakeRemoteNuGet");
            IPackageManager fakeRemotePkgMgr = CreatePackageManager(fakeRemoteNuGetDir);
            IPackage analyzerPkg =  AddPackage(fakeRemotePkgMgr, packageId, "1.0.2", analyzer.GetType().Assembly.Location);

            string localPackageDestination = TestUtils.CreateTestDirectory(this.TestContext, ".localpackages");

            // Act
            NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemotePkgMgr.LocalRepository, localPackageDestination, logger);
            AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
            bool result = apg.Generate(new Roslyn.CommandLine.NuGetReference(packageId, new SemanticVersion("1.0.2")), "cs", null, outputDir);

            // Assert
            Assert.IsTrue(result);
            string jarFilePath = AssertPluginJarExists(outputDir);

            JarInfo jarInfo = inspector.GetPluginDescription(jarFilePath);

            if (jarInfo != null)
            {
                this.TestContext.AddResultFile(jarInfo.FileName);
            }

            Assert.IsNotNull(jarInfo, "Failed to process the generated jar successfully");

            AssertExpectedManifestValue(WellKnownPluginProperties.Key, "analyzer1.pkgid1", jarInfo);

            // Check that NuGet package properties have been correctly mapped to the plugin manifest
            AssertPackagePropertiesInManifest(analyzerPkg, jarInfo);

            // Check for the expected property values required by the C# plugin
            // Property name prefixes should be lower case; the case of the value should be the same as the package id
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.analyzerId", "Analyzer1.Pkgid1", jarInfo);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.ruleNamespace", "Analyzer1.Pkgid1", jarInfo);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.nuget.packageId", "Analyzer1.Pkgid1", jarInfo);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.nuget.packageVersion", "1.0.2", jarInfo);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.staticResourceName", "Analyzer1.Pkgid1.1.0.2.zip", jarInfo);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.pluginKey", "analyzer1.pkgid1", jarInfo);
            AssertExpectedPropertyDefinitionValue("analyzer1.pkgid1.pluginVersion", "1.0.2", jarInfo);

            JarInfo.RulesDefinition rulesDefn = AssertRulesDefinitionExists(jarInfo);
            AssertRepositoryIsValid(rulesDefn.Repository);
            AssertExpectedRulesExist(analyzer, rulesDefn.Repository);
            Assert.AreEqual("roslyn.analyzer1.pkgid1", rulesDefn.Repository.Key, "Unexpected repository key");

            AssertExpectedStaticZipFileExists(jarFilePath, "static\\analyzer1.pkgid1.1.0.2.zip",
                /* zip file contents */
                "analyzers\\ExampleAnalyzer1.dll");

            Assert.AreEqual(8, jarInfo.Extensions.Count, "Unexpected number of extensions");
        }

        #region Private methods

        private PluginInspector CreatePluginInspector(Common.ILogger logger)
        {
            string tempDir = TestUtils.CreateTestDirectory(this.TestContext, "pluginInsp");
            PluginInspector inspector = new PluginInspector(logger, tempDir);

            return inspector;
        }

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

        private static void AssertExpectedPropertyDefinitionValue(string propertyName, string expected, JarInfo jarInfo)
        {
            JarInfo.PropertyDefinition actual = AssertPropertyDefinitionExists(propertyName, jarInfo);
            Assert.AreEqual(expected, actual.DefaultValue, "Property definition does not have the expected value. Property: {0}", propertyName);
        }

        private static JarInfo.PropertyDefinition AssertPropertyDefinitionExists(string propertyName, JarInfo jarInfo)
        {
            Assert.IsNotNull(jarInfo.Extensions, "Extensions should not be null");

            JarInfo.PropertyDefinition actual = jarInfo.Extensions
                .OfType<JarInfo.PropertyDefinition>()
                .FirstOrDefault(pd => string.Equals(pd.Key, propertyName, System.StringComparison.Ordinal));

            Assert.IsNotNull(actual, "Failed to find expected property: {0}", propertyName);
            AssertPropertyHasValue(actual.DefaultValue, propertyName);
            return actual;
        }

        private static JarInfo.RulesDefinition AssertRulesDefinitionExists(JarInfo jarInfo)
        {
            Assert.IsNotNull(jarInfo.Extensions, "Extensions should not be null");

            IEnumerable<JarInfo.RulesDefinition> defns = jarInfo.Extensions.OfType<JarInfo.RulesDefinition>();

            Assert.AreNotEqual(0, defns.Count(), "RulesDefinition extension does not exist");
            Assert.AreEqual(1, defns.Count(), "Multiple rules definitions exist");

            JarInfo.RulesDefinition defn = defns.Single();

            return defn;
        }

        private static void AssertRepositoryIsValid(JarInfo.Repository repository)
        {
            Assert.IsNotNull(repository, "Repository should not be null");

            AssertPropertyHasValue(repository.Key, "Repository Key");
            AssertPropertyHasValue(repository.Name, "Repository Name");

            Assert.AreEqual(SupportedLanguages.CSharp, repository.Language, "Unexpected repository language");

            Assert.IsNotNull(repository.Rules, "Repository rules should not be null");
            Assert.AreNotEqual(0, repository.Rules.Count, "Repository should have at least one rule");
        }

        private static void AssertPropertyHasValue(string value, string propertyName)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(value), "Property should have a value: {0}", propertyName);
        }

        private static void AssertExpectedRulesExist(DiagnosticAnalyzer analyzer, JarInfo.Repository repository)
        {
            foreach(DiagnosticDescriptor descriptor in analyzer.SupportedDiagnostics)
            {
                AssertRuleExists(descriptor, repository);
            }
        }

        private static void AssertRuleExists(DiagnosticDescriptor descriptor, JarInfo.Repository repository)
        {
            IEnumerable<JarInfo.Rule> matches = repository.Rules.Where(r => string.Equals(r.InternalKey, descriptor.Id, System.StringComparison.Ordinal));

            Assert.AreNotEqual(0, matches.Count(), "Failed to find expected rule: {0}", descriptor.Id);
            Assert.AreEqual(1, matches.Count(), "Multiple rules have the same id: {0}", descriptor.Id);

            JarInfo.Rule actual = matches.Single();
            Assert.AreEqual(descriptor.Title.ToString(), actual.Name, "Unexpected rule name");
            Assert.AreEqual(descriptor.Id, actual.Key, "Unexpected rule key");
            AssertPropertyHasValue(actual.Severity, "Severity");
        }

        private static void AssertPackagePropertiesInManifest(IPackage package, JarInfo jarInfo)
        {            
            AssertExpectedManifestValue("Plugin-Key", package.Id.ToLowerInvariant(), jarInfo); // plugin-key should be lowercase
            AssertExpectedManifestValue("Plugin-Name", package.Title, jarInfo);
            AssertExpectedManifestValue("Plugin-Version", package.Version.ToString(), jarInfo);
            AssertExpectedManifestValue("Plugin-Description", package.Description, jarInfo);
            AssertExpectedManifestValue("Plugin-Organization", String.Join(",", package.Owners), jarInfo);
            AssertExpectedManifestValue("Plugin-Homepage", package.ProjectUrl.ToString(), jarInfo);
            AssertExpectedManifestValue("Plugin-Developers", String.Join(",", package.Authors), jarInfo);
        }

        private static void AssertExpectedManifestValue(string propertyName, string expectedValue, JarInfo jarInfo)
        {
            Assert.IsNotNull(jarInfo.Manifest, "Manifest should not be null");

            JarInfo.ManifestItem actual = jarInfo.Manifest
                .FirstOrDefault(item => string.Equals(item.Key, propertyName, System.StringComparison.OrdinalIgnoreCase));

            Assert.IsNotNull(actual, "Failed to find expected manifest property: {0}", propertyName);
            Assert.AreEqual(expectedValue, actual.Value, "Unexpected manifest value for {0}", propertyName);
        }

        private void AssertExpectedStaticZipFileExists(string jarFilePath, string staticResourceName,
            params string[] expectedZipContents)
        {
            // Check the jar contains the expected zip file
            ZipFileChecker jarChecker = new ZipFileChecker(this.TestContext, jarFilePath);
            jarChecker.AssertZipContainsFiles(staticResourceName);

            // Now create another checker to check the contents of the zip file
            string embeddedZipFilePath = Path.Combine(jarChecker.UnzippedDirectoryPath, staticResourceName);
            ZipFileChecker embeddedFileChecker = new ZipFileChecker(this.TestContext, embeddedZipFilePath);
            embeddedFileChecker.AssertZipContainsFiles(expectedZipContents);
        }

        #endregion
    }
}
