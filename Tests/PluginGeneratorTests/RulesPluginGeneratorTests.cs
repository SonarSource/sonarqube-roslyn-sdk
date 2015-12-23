//-----------------------------------------------------------------------
// <copyright file="RulesPluginGeneratorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins;
using System.IO;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class RulesPluginGeneratorTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void RulePluginGen_Simple()
        {
            string inputDir = TestUtils.CreateTestDirectory(this.TestContext, "input");
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, "output");
            string fullJarFilePath = Path.Combine(outputDir, "myPlugin.jar");

            string rulesXmlFilePath = TestUtils.CreateTextFile("rules.xml", inputDir, "<xml Rules />");

            IJdkWrapper jdkWrapper = new JdkWrapper();
            RulesPluginBuilder generator = new RulesPluginBuilder(jdkWrapper, new TestLogger());

            PluginDefinition defn = new PluginDefinition()
            {
                Key = "MyPlugin",
                Name = "My Plugin",
                Language = "java",
                Description = "Generated plugin",
                Version = "0.1-SNAPSHOT",
                Organization = "ACME Software Ltd",
                License = "Commercial",
                Developers = typeof(RulesPluginBuilder).FullName
            };

            generator.GeneratePlugin(defn, rulesXmlFilePath, fullJarFilePath);
            if (File.Exists(fullJarFilePath))
            {
                this.TestContext.AddResultFile(fullJarFilePath);
            }

            new JarChecker(this.TestContext, fullJarFilePath)
                .JarContainsFiles(
                    "resources\\rules.xml",
                    "org\\sonarqube\\plugin\\sdk\\MyPlugin\\Plugin.class",
                    "org\\sonarqube\\plugin\\sdk\\MyPlugin\\PluginRulesDefinition.class");
        }
    }
}
