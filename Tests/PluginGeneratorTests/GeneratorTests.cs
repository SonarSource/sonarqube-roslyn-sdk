using Microsoft.VisualStudio.TestTools.UnitTesting;
using PluginGenerator;
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

            IJdkWrapper jdkWrapper = new JdkWrapper();
            Generator generator = new Generator(jdkWrapper);


            PluginDefinition defn = new PluginDefinition()
            {
                Key = "MyPlugin",
                Name = "My Plugin",
                Language = "java"
            };

            bool success = generator.GeneratePlugin(defn, outputDir, new TestLogger());

            Assert.IsTrue(success, "Expecting compilation to have succeeded");

        }

    }
}
