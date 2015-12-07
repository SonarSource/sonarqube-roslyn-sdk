//-----------------------------------------------------------------------
// <copyright file="ArgumentProcessorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Roslyn.CommandLine;
using System.IO;
using Tests.Common;
using TestUtilities;

namespace SonarQube.Plugins.Roslyn.PluginGeneratorTests
{
    [TestClass]
    public class ArgumentProcessorTests
    {
        public TestContext TestContext { get; set; }

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

            AssertArgumentsProcessed(actualArgs, logger, "testing.id.no.version", null, null);

            // 2. Id and version
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer:testing.id.with.version:1.0.0-rc1" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "testing.id.with.version", "1.0.0-rc1", null);

            // 3. Id containing a colon, with version
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer:id.with:colon:2.1.0" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "id.with:colon", "2.1.0", null);
        }


        [TestMethod]
        public void ArgProc_SqaleFile()
        {
            // 0. Setup
            TestLogger logger;
            string[] rawArgs;
            ProcessedArgs actualArgs;

            // 1. No sqale file value -> valid
            logger = new TestLogger();
            rawArgs = new string[] { "/a:validId" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "validId", null, null);

            // 2. Missing sqale file
            logger = new TestLogger();
            rawArgs = new string[] { "/s:missingFile.txt", "/a:validId" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
            logger.AssertSingleErrorExists("missingFile.txt"); // should be an error containing the missing file name

            // 3. Existing sqale file
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = TestUtils.CreateTextFile("valid.sqale.txt", testDir, "sqale file contents");
            
            logger = new TestLogger();
            rawArgs = new string[] { "/s:" + filePath,  "/a:valid:1.0" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "valid", "1.0", filePath);
        }

        #endregion

        #region Checks

        private static void AssertArgumentsNotProcessed(ProcessedArgs actualArgs, TestLogger logger)
        {
            Assert.IsNull(actualArgs, "Not expecting the arguments to have been processed successfully");
            logger.AssertErrorsLogged();
        }

        private static void AssertArgumentsProcessed(ProcessedArgs actualArgs, TestLogger logger, string expectedId, string expectedVersion, string expectedSqale)
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

            Assert.AreEqual(expectedSqale, actualArgs.SqaleFilePath, "Unexpected sqale file path");
            if (expectedSqale != null)
            {
                Assert.IsTrue(File.Exists(expectedSqale), "Specified sqale file should exist: {0}", expectedSqale);
            }

            logger.AssertErrorsLogged(0);
        }

        #endregion
    }
}
