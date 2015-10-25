using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.IO.Compression;
using Tests.Common;

namespace Roslyn.SonarQube.PluginGeneratorTests
{
    internal class JarChecker
    {
        private string unzippedDir;

        public JarChecker(TestContext testContext, string fullJarPath)
        {
            TestUtils.AssertFileExists(fullJarPath);

            this.unzippedDir = TestUtils.CreateTestDirectory(testContext, "unzipped");
            ZipFile.ExtractToDirectory(fullJarPath, this.unzippedDir);
        }

        public void JarContainsFiles(params string[] expectedRelativePaths)
        {
            foreach (string relativePath in expectedRelativePaths)
            {
                string fullFilePath = Path.Combine(this.unzippedDir, relativePath);
                Assert.IsTrue(File.Exists(fullFilePath), "Jar does not contain expected file: {0}", relativePath);
            }
        }

    }
}
