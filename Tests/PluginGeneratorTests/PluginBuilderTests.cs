using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube.PluginGenerator;
using System.IO;
using Tests.Common;
using TestUtilities;

namespace Roslyn.SonarQube.PluginGeneratorTests
{
    [TestClass]
    public class PluginBuilderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void PluginBuilder_Simple()
        {
            string inputDir = TestUtils.CreateTestDirectory(this.TestContext, "input");
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, "output");

            string pluginFilePath = Path.Combine(outputDir, "plugin1.jar");
            string source1 = TestUtils.CreateTextFile("Program.java", inputDir,
@"package myorg.app1;

public final class Program
{
    public final void Main(int[] args)
    {
        System.out.println(""testing..."");
    }
}
");

            PluginBuilder builder = new PluginBuilder(new TestLogger());

            builder
                .AddSourceFile(source1)
                .SetJarFilePath(pluginFilePath)
                .SetProperty("Property1", "prop 1 value")
                .Build();

            TestUtils.AssertFileExists(pluginFilePath);

            this.TestContext.AddResultFile(pluginFilePath);
        }

        [TestMethod]
        public void PluginBuilder_InvalidSource()
        {
            string inputDir = TestUtils.CreateTestDirectory(this.TestContext, "input");
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, "output");

            string pluginFilePath = Path.Combine(outputDir, "plugin1.jar");
            string source1 = TestUtils.CreateTextFile("Program.java", inputDir, "invalid java code");

            PluginBuilder builder = new PluginBuilder(new TestLogger());

            builder
                .AddSourceFile(source1)
                .SetJarFilePath(pluginFilePath)
                .SetProperty("Property1", "prop 1 value");

            AssertException.Expect<CompilerException>(() => builder.Build());
        }

    }
}
