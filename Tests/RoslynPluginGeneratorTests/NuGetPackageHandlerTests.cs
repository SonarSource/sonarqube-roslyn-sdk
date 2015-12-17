using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class NuGetPackageHandlerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestDependencyResolutionFailure()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(logger);

            // Act
            // Fetch a package that should fail due to pre-release dependencies
            handler.FetchPackage(AnalyzerPluginGenerator.NuGetPackageSource, "codeCracker", null, testDir);

            // Assert
            // No files should have been downloaded
            Assert.IsTrue(Directory.GetFiles(testDir).Length == 0);
        }
    }
}
