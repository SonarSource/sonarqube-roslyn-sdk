using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube.AnalyzerPlugins.CommandLine;
using TestUtilities;

namespace Roslyn.SonarQube.RoslynPluginGeneratorTests
{
    [TestClass]
    public class ArgumentProcessorTests
    {
        #region Tests

        [TestMethod]
        public void ArgProc_EmptyArgs()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string[] rawArgs = { };

            // Act
            ProcessedArgs actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            // Assert
            AssertArgumentsNotProcessed(actualArgs, logger);            
        }

        [TestMethod]
        public void ArgProc_AnalyzerRef_Invalid()
        {
            // 0. Setup
            TestLogger logger;
            string[] rawArgs;
            ProcessedArgs actualArgs;

            // 1. No value
            logger = new TestLogger();
            rawArgs = new string [] { "/analyzer:" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 2. Id and missing version
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer:testing.id.missing.version:" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 3. Id and invalid version
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer:testing.id.invalid.version:1.0.-invalid.version.1" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 4. Missing id
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer::2.1.0" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
        }

        [TestMethod]
        public void ArgProc_AnalyzerRef_Valid()
        {
            // 0. Setup
            TestLogger logger;
            string[] rawArgs;
            ProcessedArgs actualArgs;

            // 1. Id but no version
            logger = new TestLogger();
            rawArgs = new string[] { "/a:testing.id.no.version" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "testing.id.no.version", null);

            // 2. Id and version
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer:testing.id.with.version:1.0.0-rc1" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "testing.id.with.version", "1.0.0-rc1");

            // 3. Id containing a colon, with version
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer:id.with:colon:2.1.0" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "id.with:colon", "2.1.0");
        }

        #endregion

        #region Checks

        private static void AssertArgumentsNotProcessed(ProcessedArgs actualArgs, TestLogger logger)
        {
            Assert.IsNull(actualArgs, "Not expecting the arguments to have been processed successfully");
            logger.AssertErrorsLogged();
        }

        private static void AssertArgumentsProcessed(ProcessedArgs actualArgs, TestLogger logger, string expectedId, string expectedVersion)
        {
            Assert.IsNotNull(actualArgs, "Expecting the arguments to have been processed successfully");

            Assert.IsNotNull(actualArgs.AnalyzerRef, "Not expecting the analyzer reference to be null");
            Assert.AreEqual(actualArgs.AnalyzerRef.PackageId, expectedId, "Unexpected package id returned");

            NuGetReference actualRef = actualArgs.AnalyzerRef;
            if (expectedVersion == null)
            {
                Assert.IsNull(actualRef.Version, "Expecting the version to be null");
            }
            else
            {
                Assert.IsNotNull(actualRef.Version, "Not expecting the version to be null");
                Assert.AreEqual(expectedVersion, actualRef.Version.ToString());
            }

            logger.AssertErrorsLogged(0);
        }

        #endregion
    }
}
