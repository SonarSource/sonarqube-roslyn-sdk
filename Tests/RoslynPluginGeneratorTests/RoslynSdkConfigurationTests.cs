//-----------------------------------------------------------------------
// <copyright file="RoslynSdkConfigurationTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;

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
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = Path.Combine(testDir, "original.txt");


            RoslynSdkConfiguration config = new RoslynSdkConfiguration();

            config.PluginKeyDifferentiator = "diff";
            config.RepositoryKey = "key";
            config.RepositoryName = "repo.name";
            config.RepositoryLanguage = "language";
            config.RulesXmlResourcePath = "rulesPath";
            config.SqaleXmlResourcePath = "sqalePath";

            config.Properties["prop1.key"] = "value1";
            config.Properties["prop2.key"] = "value2";

            // Save and check 
            config.Save(filePath);
            Assert.AreEqual(filePath, config.FileName);
            this.TestContext.AddResultFile(filePath);

            // Reload and check
            RoslynSdkConfiguration reloaded = RoslynSdkConfiguration.Load(filePath);

            Assert.IsNotNull(reloaded);

            Assert.AreEqual("diff", reloaded.PluginKeyDifferentiator);
            Assert.AreEqual("key", reloaded.RepositoryKey);
            Assert.AreEqual("repo.name", reloaded.RepositoryName);
            Assert.AreEqual("language", reloaded.RepositoryLanguage);
            Assert.AreEqual("rulesPath", reloaded.RulesXmlResourcePath);
            Assert.AreEqual("sqalePath", reloaded.SqaleXmlResourcePath);

            Assert.AreEqual(2, reloaded.Properties.Count);
            AssertPropertyExists("prop1.key", "value1", reloaded.Properties);
            AssertPropertyExists("prop2.key", "value2", reloaded.Properties);
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
  <SqaleXmlResourcePath>/org/sonar/plugins/roslynsdk/sqale.xml</SqaleXmlResourcePath>
  <PluginProperties>
    <example.pluginKey>example</example.pluginKey>
    <example.pluginVersion>example</example.pluginVersion>
    <example.staticResourceName>example</example.staticResourceName>
    <example.nuget.packageId>example</example.nuget.packageId>
    <example.nuget.packageVersion>example</example.nuget.packageVersion>
    <example.analyzerId>example</example.analyzerId>
    <example.ruleNamespace>example</example.ruleNamespace>
  </PluginProperties>
</RoslynSdkConfiguration>
";

            #endregion

            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = TestUtils.CreateTextFile("realPluginProperties.txt", testDir, exampleConfig);
            this.TestContext.AddResultFile(filePath);

            // Act
            RoslynSdkConfiguration loaded = RoslynSdkConfiguration.Load(filePath);
            string resavedFilePath = Path.Combine(testDir, "resaved.txt");
            loaded.Save(resavedFilePath);
            this.TestContext.AddResultFile(resavedFilePath);

            // Assert
            Assert.AreEqual("example", loaded.PluginKeyDifferentiator);
            Assert.AreEqual("roslyn.example", loaded.RepositoryKey);
            Assert.AreEqual("example", loaded.RepositoryLanguage);
            Assert.AreEqual("example", loaded.RepositoryName);
            Assert.AreEqual("/org/sonar/plugins/roslynsdk/rules.xml", loaded.RulesXmlResourcePath);
            Assert.AreEqual("/org/sonar/plugins/roslynsdk/sqale.xml", loaded.SqaleXmlResourcePath);

            AssertPropertyExists("example.pluginKey", "example", loaded.Properties);
            AssertPropertyExists("example.pluginVersion", "example", loaded.Properties);
            AssertPropertyExists("example.staticResourceName", "example", loaded.Properties);
            AssertPropertyExists("example.nuget.packageId", "example", loaded.Properties);
            AssertPropertyExists("example.nuget.packageVersion", "example", loaded.Properties);
            AssertPropertyExists("example.analyzerId", "example", loaded.Properties);
            AssertPropertyExists("example.ruleNamespace", "example", loaded.Properties);
        }

        #endregion

        #region Private methods

        private static void AssertPropertyExists(string expectedKey, string expectedValue, PluginProperties actualProperties)
        {
            Assert.IsTrue(actualProperties.ContainsKey(expectedKey), "Expected key not found: {0}", expectedKey);
            Assert.AreEqual(expectedValue, actualProperties[expectedKey], "Unexpected value for key '{0}'", expectedKey);
        }

        #endregion

    }
}
