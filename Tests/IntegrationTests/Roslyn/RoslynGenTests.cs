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
using SonarQube.Plugins.Common;
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
			RoslynPlugin_GenerateForValidAnalyzer_Succeeds(new RoslynAnalyzer11.CSharpAnalyzer(), "RoslynAnalyzer11.dll");
		}

		[TestMethod]
		public void RoslynPlugin_GenerateForValidAnalyzer_Succeeds_For_Modern_Analyzer()
		{
			RoslynPlugin_GenerateForValidAnalyzer_Succeeds(new RoslynAnalyzer3.CSharpAnalyzer(), "RoslynAnalyzer3.dll");
		}

		public void RoslynPlugin_GenerateForValidAnalyzer_Succeeds(DiagnosticAnalyzer analyzer, string analyzerAssemblyName)
		{
			// Arrange
			TestLogger logger = new TestLogger();
			string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");


			string packageId = "Analyzer1.Pkgid1"; // package id is not all lowercase
			string fakeRemoteNuGetDir = TestUtils.CreateTestDirectory(TestContext, ".fakeRemoteNuGet");
			IPackageManager fakeRemotePkgMgr = CreatePackageManager(fakeRemoteNuGetDir);
			IPackage analyzerPkg = AddPackage(fakeRemotePkgMgr, packageId, "1.0.2", analyzer.GetType().Assembly.Location);

			string localPackageDestination = TestUtils.CreateTestDirectory(TestContext, ".localpackages");

			// Act
			NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemotePkgMgr.LocalRepository, localPackageDestination, logger);
			AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
			ProcessedArgs args = new ProcessedArgs(packageId, new SemanticVersion("1.0.2"), "cs", null, false, false, outputDir, null);
			bool result = apg.Generate(args);

			// Assert
			result.Should().BeTrue();

			// Expecting one plugin per dependency with analyzers
			CheckJarGeneratedForPackage(outputDir, analyzer, analyzerPkg, analyzerAssemblyName);
			AssertJarsGenerated(outputDir, 1);
		}

		// Test verifies https://jira.sonarsource.com/browse/SFSRAP-32
		[TestMethod]
		public void RoslynPlugin_GenerateForDependencyAnalyzers_Succeeds()
		{
			RoslynPlugin_GenerateForDependencyAnalyzers_Succeeds(new RoslynAnalyzer11.CSharpAnalyzer(), "RoslynAnalyzer11.dll");
		}


		[TestMethod]
		public void RoslynPlugin_GenerateForDependencyAnalyzers_Succeeds_For_Modern_Analyzer()
		{
			RoslynPlugin_GenerateForDependencyAnalyzers_Succeeds(new RoslynAnalyzer3.CSharpAnalyzer(), "RoslynAnalyzer3.dll");
		}

		public void RoslynPlugin_GenerateForDependencyAnalyzers_Succeeds(DiagnosticAnalyzer analyzer, string analyzerAssemblyName)
		{
			// Arrange
			TestLogger logger = new TestLogger();
			string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
			string dummyContentFile = TestUtils.CreateTextFile("dummy.txt", outputDir, "non-analyzer content file");

			string fakeRemoteNuGetDir = TestUtils.CreateTestDirectory(TestContext, ".fakeRemoteNuGet");
			IPackageManager fakeRemotePkgMgr = CreatePackageManager(fakeRemoteNuGetDir);
			IPackage child1 = AddPackage(fakeRemotePkgMgr, "Analyzer.Child1", "1.1.0", analyzer.GetType().Assembly.Location);
			IPackage child2 = AddPackage(fakeRemotePkgMgr, "Analyzer.Child2", "1.2.0", analyzer.GetType().Assembly.Location);
			IPackage targetPkg = AddPackage(fakeRemotePkgMgr, "Empty.Parent", "1.0.0", dummyContentFile, child1, child2);

			string localPackageDestination = TestUtils.CreateTestDirectory(TestContext, ".localpackages");

			// Act
			NuGetPackageHandler nuGetHandler = new NuGetPackageHandler(fakeRemotePkgMgr.LocalRepository, localPackageDestination, logger);
			AnalyzerPluginGenerator apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
			ProcessedArgs args = new ProcessedArgs(targetPkg.Id, targetPkg.Version, "cs", null, false,
				true /* generate plugins for dependencies with analyzers*/, outputDir, null);
			bool result = apg.Generate(args);

			// Assert
			result.Should().BeTrue();

			// Expecting one plugin per dependency with analyzers
			CheckJarGeneratedForPackage(outputDir, analyzer, child1, analyzerAssemblyName);
			CheckJarGeneratedForPackage(outputDir, analyzer, child2, analyzerAssemblyName);
			AssertJarsGenerated(outputDir, 2);
		}

		[TestMethod]
		public void RoslynPlugin_GenerateForMultiLevelAnalyzers_Succeeds()
		{
			RoslynPlugin_GenerateForMultiLevelAnalyzers_Succeeds(new RoslynAnalyzer11.CSharpAnalyzer(), "RoslynAnalyzer11.dll");
		}

		[TestMethod]
		public void RoslynPlugin_GenerateForMultiLevelAnalyzers_Succeeds_For_Modern_Analyzer()
		{
			RoslynPlugin_GenerateForMultiLevelAnalyzers_Succeeds(new RoslynAnalyzer3.CSharpAnalyzer(), "RoslynAnalyzer3.dll");
		}

		public void RoslynPlugin_GenerateForMultiLevelAnalyzers_Succeeds(DiagnosticAnalyzer analyzer, string analyzerAssemblyName)
		{
			// Arrange
			TestLogger logger = new TestLogger();
			string outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");


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
			ProcessedArgs args = new ProcessedArgs(targetPkg.Id, targetPkg.Version, "cs", null, false,
				true /* generate plugins for dependencies with analyzers*/, outputDir, null);
			bool result = apg.Generate(args);

			// Assert
			result.Should().BeTrue();

			// Expecting one plugin per dependency with analyzers
			CheckJarGeneratedForPackage(outputDir, analyzer, targetPkg, analyzerAssemblyName);
			CheckJarGeneratedForPackage(outputDir, analyzer, child1, analyzerAssemblyName);
			CheckJarGeneratedForPackage(outputDir, analyzer, child2, analyzerAssemblyName);
			AssertJarsGenerated(outputDir, 3);
		}

		[TestMethod]
		public void RoslynPlugin_ThirdPartyAnalyzer_1_Succeeds()
		{
			// Note: this test will access the public nuget site if the package
			// can't be found locally so:
			// 1) it can be slow and
			// 2) it it more likely to fail due to environmental issues
			CheckCanGenerateForThirdPartyAssembly("wintellect.analyzers", new SemanticVersion("1.0.6"));
		}

		[TestMethod]
		public void RoslynPlugin_ThirdPartyAnalyzer_2_Succeeds()
		{
			// Build against the latest public version (currently v5.6)
			// Note: this test could fail if the third-party analyzer is updated
			// to use a newer version of Roslyn than the one supported by the SDK
			CheckCanGenerateForThirdPartyAssembly("RefactoringEssentials", null /* latest */);
		}

		[TestMethod]
		public void RoslynPlugin_ThirdPartyAnalyzer_3_Succeeds()
		{
			// Build against the latest public version (currently v1.0.593)
			// Note: this test could fail if the third-party analyzer is updated
			// to use a newer version of Roslyn than the one supported by the SDK
			CheckCanGenerateForThirdPartyAssembly("Meziantou.Analyzer", null /* latest */);
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

		private void CheckCanGenerateForThirdPartyAssembly(string packageId, SemanticVersion version)
		{
			// Arrange
			var logger = new TestLogger();
			var outputDir = TestUtils.CreateTestDirectory(TestContext, ".out");
			var localPackageDestination = TestUtils.CreateTestDirectory(TestContext, ".localpackages");
			var localRepoWithRemotePackage = InstallRemotePackageLocally(packageId, version);

			// Act
			var nuGetHandler = new NuGetPackageHandler(localRepoWithRemotePackage, localPackageDestination, logger);

			var apg = new AnalyzerPluginGenerator(nuGetHandler, logger);
			var args = new ProcessedArgs(packageId, version, "cs", null, false, false, outputDir, null);
			var result = apg.Generate(args);

			// Assert
			result.Should().BeTrue();

			// Expecting one plugin per dependency with analyzers
			AssertJarsGenerated(outputDir, 1);

			var jarFilePath = Directory.GetFiles(outputDir, "*.jar", SearchOption.TopDirectoryOnly).Single();
			var jarChecker = new ZipFileChecker(TestContext, jarFilePath);
			var actualManifestFilePath = jarChecker.AssertFileExists("META-INF\\MANIFEST.MF");

			JarManifestReader reader = new JarManifestReader(File.ReadAllText(actualManifestFilePath));
			AssertFixedValuesInManifest(reader);
		}

		/// <summary>
		/// Installs the specified package from the public NuGet feed to the
		/// local machine, and returns the local package repo that provides
		/// access to the package.
		/// </summary>
		private IPackageRepository InstallRemotePackageLocally(string packageId, SemanticVersion version)
		{
			TestContext.WriteLine($"Test setup: installing package locally - {packageId}, {version?.ToString() ?? "{{version not specified}}"}");

			// Note: using the same shared cache location as for the SDK itself to reduce
			// the number of times the package needs to be pulled from the public feed.
			// The remote feed will only be used if the package cannot be found locally.
			var sdkPackageDestination = Utilities.CreateTempDirectory(".nuget");
			TestContext.WriteLine($"Test setup: local shared repo directory: {sdkPackageDestination}");

			var repo = PackageRepositoryFactory.Default.CreateRepository("https://www.nuget.org/api/v2/");
			var packageManager = new PackageManager(repo, sdkPackageDestination);
			try
			{
				packageManager.InstallPackage(packageId, version);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Test setup error: failed to install NuGet package locally. Error: {ex.ToString()}");
			}

			TestContext.WriteLine($"Test setup: package installed.");
			return packageManager.LocalRepository;
		}

		#endregion Private methods

		#region Checks

		private static string[] GetGeneratedJars(string rootDir)
		{
			return Directory.GetFiles(rootDir, "*.jar", SearchOption.TopDirectoryOnly);
		}

		private void CheckJarGeneratedForPackage(string rootDir, DiagnosticAnalyzer analyzer, IPackage package, string analyzerAssemblyName)
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

			var manifestReader = new JarManifestReader(File.ReadAllText(actualManifestFilePath));
			manifestReader.FindValue(WellKnownPluginProperties.Key).Should().Be(pluginId.Replace(".", ""));

			AssertPackagePropertiesInManifest(package, manifestReader);
			AssertFixedValuesInManifest(manifestReader);

			// Check the rules
			string actualRuleFilePath = jarChecker.AssertFileExists("." + config.RulesXmlResourcePath);
			AssertExpectedRulesExist(analyzer, actualRuleFilePath);

			// Now create another checker to check the contents of the zip file (strict check this time)
			CheckEmbeddedAnalyzerPayload(jarChecker, "static\\" + pluginId + "." + package.Version + ".zip",
				/* zip file contents */
				$"analyzers\\{analyzerAssemblyName}");
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

		private static void AssertPackagePropertiesInManifest(IPackage package, JarManifestReader manifestReader)
		{
			manifestReader.FindValue("Plugin-Name").Should().Be(package.Title);
			manifestReader.FindValue("Plugin-Version").Should().Be(package.Version.ToString());
			manifestReader.FindValue("Plugin-Description").Should().Be(package.Description);
			manifestReader.FindValue("Plugin-Organization").Should().Be(String.Join(",", package.Owners));
			manifestReader.FindValue("Plugin-Homepage").Should().Be(package.ProjectUrl.ToString());
			manifestReader.FindValue("Plugin-Developers").Should().Be(String.Join(",", package.Authors));
			manifestReader.FindValue("Plugin-TermsConditionsUrl").Should().Be(package.LicenseUrl.ToString());
		}

		private static void AssertFixedValuesInManifest(JarManifestReader reader)
		{
			reader.FindValue("Sonar-Version").Should().Be("6.7");
			reader.FindValue("Plugin-Class").Should().Be("org.sonar.plugins.roslynsdk.RoslynSdkGeneratedPlugin");
			reader.FindValue("SonarLint-Supported").Should().Be("false");
			reader.FindValue("Plugin-Dependencies").Should().Be("META-INF/lib/jsr305-1.3.9.jar META-INF/lib/commons-io-2.6.jar META-INF/lib/stax2-api-3.1.4.jar META-INF/lib/staxmate-2.0.1.jar META-INF/lib/stax-api-1.0.1.jar");
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