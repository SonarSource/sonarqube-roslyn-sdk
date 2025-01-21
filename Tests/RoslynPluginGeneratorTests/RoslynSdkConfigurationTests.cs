/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource SA
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

using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class RoslynSdkConfigurationTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void SdkConfig_SaveAndReload_Succeeds()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(TestContext);
            string filePath = Path.Combine(testDir, "original.txt");

            RoslynSdkConfiguration config = new RoslynSdkConfiguration
            {
                PluginKeyDifferentiator = "diff",
                RepositoryKey = "key",
                RepositoryName = "repo.name",
                RepositoryLanguage = "language",
                RulesXmlResourcePath = "rulesPath",
            };

            config.Properties["prop1.Key"] = "value1";
            config.Properties["prop2.Key"] = "value2";

            // Save and check
            config.Save(filePath);
            filePath.Should().Be(config.FileName);
            TestContext.AddResultFile(filePath);

            // Reload and check
            RoslynSdkConfiguration reloaded = RoslynSdkConfiguration.Load(filePath);

            reloaded.Should().NotBeNull();

            reloaded.PluginKeyDifferentiator.Should().Be("diff");
            reloaded.RepositoryKey.Should().Be("key");
            reloaded.RepositoryName.Should().Be("repo.name");
            reloaded.RepositoryLanguage.Should().Be("language");
            reloaded.RulesXmlResourcePath.Should().Be("rulesPath");

            reloaded.Properties.Count.Should().Be(2);
            AssertPropertyExists("prop1.Key", "value1", reloaded.Properties);
            AssertPropertyExists("prop2.Key", "value2", reloaded.Properties);
        }

        [TestMethod]
        public void SdkConfig_LoadRealExample_Succeeds()
        {
            // Arrange

            #region File content

            string exampleConfig = @"<RoslynSdkConfiguration>
  <PluginKeyDifferentiator>example</PluginKeyDifferentiator>
  <RepositoryKey>roslyn.example</RepositoryKey>
  <RepositoryLanguage>example</RepositoryLanguage>
  <RepositoryName>example</RepositoryName>
  <RulesXmlResourcePath>/org/sonar/plugins/roslynsdk/rules.xml</RulesXmlResourcePath>
  <PluginProperties>
    <example.pluginKey>example.pluginKey.Value</example.pluginKey>
    <example.pluginVersion>example.pluginVersion.Value</example.pluginVersion>
    <example.staticResourceName>example.staticResourceName.Value</example.staticResourceName>
    <example.nuget.packageId>example.nuget.packageId.Value</example.nuget.packageId>
    <example.nuget.packageVersion>example.nuget.packageVersion.Value</example.nuget.packageVersion>
    <example.analyzerId>example.analyzerId.Value</example.analyzerId>
    <example.ruleNamespace>example.ruleNamespace.Value</example.ruleNamespace>
  </PluginProperties>
</RoslynSdkConfiguration>
";

            #endregion File content

            string testDir = TestUtils.CreateTestDirectory(TestContext);
            string filePath = TestUtils.CreateTextFile("realPluginProperties.txt", testDir, exampleConfig);
            TestContext.AddResultFile(filePath);

            // Act
            RoslynSdkConfiguration loaded = RoslynSdkConfiguration.Load(filePath);
            string resavedFilePath = Path.Combine(testDir, "resaved.txt");
            loaded.Save(resavedFilePath);
            TestContext.AddResultFile(resavedFilePath);

            // Assert
            loaded.PluginKeyDifferentiator.Should().Be("example");
            loaded.RepositoryKey.Should().Be("roslyn.example");
            loaded.RepositoryLanguage.Should().Be("example");
            loaded.RepositoryName.Should().Be("example");
            loaded.RulesXmlResourcePath.Should().Be("/org/sonar/plugins/roslynsdk/rules.xml");

            AssertPropertyExists("example.pluginKey", "example.pluginKey.Value", loaded.Properties);
            AssertPropertyExists("example.pluginVersion", "example.pluginVersion.Value", loaded.Properties);
            AssertPropertyExists("example.staticResourceName", "example.staticResourceName.Value", loaded.Properties);
            AssertPropertyExists("example.nuget.packageId", "example.nuget.packageId.Value", loaded.Properties);
            AssertPropertyExists("example.nuget.packageVersion", "example.nuget.packageVersion.Value", loaded.Properties);
            AssertPropertyExists("example.analyzerId", "example.analyzerId.Value", loaded.Properties);
            AssertPropertyExists("example.ruleNamespace", "example.ruleNamespace.Value", loaded.Properties);
        }

        #endregion Tests

        #region Private methods

        private static void AssertPropertyExists(string expectedKey, string expectedValue, PluginProperties actualProperties)
        {
            // Note: we're explicitly searching for the key using Linq so we can be sure a case-sensitive match is being used
            string match = actualProperties.Keys.OfType<string>().FirstOrDefault(k => string.Equals(expectedKey, k, System.StringComparison.Ordinal));
            match.Should().NotBeNull("Expected key not found: {0}", expectedKey);

            expectedValue.Should().Be(actualProperties[expectedKey], "Unexpected value for key '{0}'", expectedKey);
        }

        #endregion Private methods
    }
}