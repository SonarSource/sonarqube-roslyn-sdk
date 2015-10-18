using Microsoft.VisualStudio.TestTools.UnitTesting;
using PluginGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Common;

namespace PluginGeneratorTests
{
    [TestClass]
    public class JarBuilderTests
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void JarBuilder_Layout()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string inputsDir = TestUtils.CreateTestDirectory(this.TestContext, "inputs");
            string layoutDir = TestUtils.CreateTestDirectory(this.TestContext, "jarLayout");

            string file1 = TestUtils.CreateTextFile("file1.txt", inputsDir, "file1 content");
            string file2 = TestUtils.CreateTextFile("file2.txt", inputsDir, "file2 content");
            string file3 = TestUtils.CreateTextFile("file3.txt", inputsDir, "file3 content");

            // Act
            JarBuilder builder = new JarBuilder(logger);
            builder.SetManifestPropety("prop1", "prop1 value");
            builder.SetManifestPropety("prop2", "prop2 value");
            builder.SetManifestPropety("prop3", "prop3 value");

            builder.AddFile(file1, null);
            builder.AddFile(file2, "myorg\\myapp\\f2.txt");
            builder.AddFile(file3, "resources\\f3.txt");

            builder.LayoutFiles(layoutDir);

            // Assert
            string content = TestUtils.AssertFileExists(JarBuilder.MANIFEST_FILE_NAME, layoutDir);
            AssertManifestPropertyExists(content, "prop1", "prop1 value");
            AssertManifestPropertyExists(content, "prop2", "prop2 value");
            AssertManifestPropertyExists(content, "prop3", "prop3 value");

            content = TestUtils.AssertFileExists("file1.txt", layoutDir);
            Assert.AreEqual("file1 content", content, "Unexpected file content");

            content = TestUtils.AssertFileExists("myorg\\myapp\\f2.txt", layoutDir);
            Assert.AreEqual("file2 content", content, "Unexpected file content");

            content = TestUtils.AssertFileExists("resources\\f3.txt", layoutDir);
            Assert.AreEqual("file3 content", content, "Unexpected file content");
        }

        private static void AssertManifestPropertyExists(string content, string key, string value)
        {
            string expected = key + "=" + value;
            Assert.IsTrue(content.Contains(expected), "Setting missing from manifest: {0}", expected);
        }
    }
}
