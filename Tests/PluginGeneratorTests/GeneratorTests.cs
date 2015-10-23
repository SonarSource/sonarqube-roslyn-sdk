using Microsoft.VisualStudio.TestTools.UnitTesting;
using PluginGenerator;
using System.IO;
using Tests.Common;

namespace PluginGeneratorTests
{
    [TestClass]
    public class GeneratorTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PluginGen_Simple()
        {
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, "output");
            string fullJarFilePath = Path.Combine(outputDir, "myPlugin.jar");

            IJdkWrapper jdkWrapper = new JdkWrapper();
            Generator generator = new Generator(jdkWrapper, new TestLogger());

            PluginDefinition defn = new PluginDefinition()
            {
                Key = "MyPlugin",
                Name = "My Plugin",
                Language = "java",
                Description = "Generated plugin",
                Version = "0.1-SNAPSHOT",
                Organization = "ACME Software Ltd",
                License = "Commercial",
                Developers = typeof(Generator).FullName
            };

            generator.GeneratePlugin(defn, fullJarFilePath);
            if (File.Exists(fullJarFilePath))
            {
                this.TestContext.AddResultFile(fullJarFilePath);
            }

            TestUtils.AssertFileExists(fullJarFilePath);
        }

    }
}
