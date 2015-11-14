using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube.PluginGenerator;
using System.IO;
using Tests.Common;
using TestUtilities;

namespace Roslyn.SonarQube.PluginGeneratorTests
{
    [TestClass]
    public class RulePluginGeneratorTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PluginGen_Simple()
        {
            string inputDir = TestUtils.CreateTestDirectory(this.TestContext, "input");
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, "output");
            string fullJarFilePath = Path.Combine(outputDir, "myPlugin.jar");

            string rulesXmlFilePath = TestUtils.CreateTextFile("rules.xml", inputDir, "<xml Rules />");

            IJdkWrapper jdkWrapper = new JdkWrapper();
            RulesPluginGenerator generator = new RulesPluginGenerator(jdkWrapper, new TestLogger());

            PluginDefinition defn = new PluginDefinition()
            {
                Key = "MyPlugin",
                Name = "My Plugin",
                Language = "java",
                Description = "Generated plugin",
                Version = "0.1-SNAPSHOT",
                Organization = "ACME Software Ltd",
                License = "Commercial",
                Developers = typeof(RulesPluginGenerator).FullName
            };

            generator.GeneratePlugin(defn, rulesXmlFilePath, fullJarFilePath);
            if (File.Exists(fullJarFilePath))
            {
                this.TestContext.AddResultFile(fullJarFilePath);
            }

            new JarChecker(this.TestContext, fullJarFilePath)
                .JarContainsFiles(
                    "resources\\rules.xml",
                    "myorg\\MyPlugin\\Plugin.class",
                    "myorg\\MyPlugin\\PluginRulesDefinition.class");
        }

    }
}
