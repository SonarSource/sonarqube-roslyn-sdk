/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Roslyn.CommandLine;
using SonarQube.Plugins.Test.Common;
using System.IO;

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

            AssertArgumentsProcessed(actualArgs, logger, "testing.id.no.version", null, null, false);

            // 2. Id and version
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer:testing.id.with.version:1.0.0-rc1" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "testing.id.with.version", "1.0.0-rc1", null, false);

            // 3. Id containing a colon, with version
            logger = new TestLogger();
            rawArgs = new string[] { "/analyzer:id.with:colon:2.1.0" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "id.with:colon", "2.1.0", null, false);
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

            AssertArgumentsProcessed(actualArgs, logger, "validId", null, null, false);

            // 2. Missing sqale file
            logger = new TestLogger();
            rawArgs = new string[] { "/sqale:missingFile.txt", "/a:validId" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
            logger.AssertSingleErrorExists("missingFile.txt"); // should be an error containing the missing file name

            // 3. Existing sqale file
            string testDir = TestUtils.CreateTestDirectory(TestContext);
            string filePath = TestUtils.CreateTextFile("valid.sqale.txt", testDir, "sqale file contents");

            logger = new TestLogger();
            rawArgs = new string[] { "/sqale:" + filePath,  "/a:valid:1.0" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "valid", "1.0", filePath, false);
        }

        [TestMethod]
        public void ArgProc_RuleFile()
        {
            // 0. Setup
            TestLogger logger;
            string[] rawArgs;
            ProcessedArgs actualArgs;

            // 1. No rule file value -> valid
            logger = new TestLogger();
            rawArgs = new string[] { "/a:validId" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            actualArgs.RuleFilePath.Should().BeNull();

            // 2. Missing rule file
            logger = new TestLogger();
            rawArgs = new string[] { "/rules:missingFile.txt", "/a:validId" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
            logger.AssertSingleErrorExists("missingFile.txt"); // should be an error containing the missing file name

            // 3. Existing rule file
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = TestUtils.CreateTextFile("valid.rules.txt", testDir, "rule file contents");

            logger = new TestLogger();
            rawArgs = new string[] { $"/rules:{filePath}", "/a:valid:1.0" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            actualArgs.Should().NotBeNull();
            actualArgs.RuleFilePath.Should().Be(filePath);
        }

        [TestMethod]
        public void ArgProc_AcceptLicenses_Valid()
        {
            // 0. Setup
            TestLogger logger;
            string[] rawArgs;
            ProcessedArgs actualArgs;

            // 1. Correct argument -> valid and accept is true
            logger = new TestLogger();
            rawArgs = new string[] { "/a:validId", "/acceptLicenses" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "validId", null, null, true);
        }

        [TestMethod]
        public void ArgProc_AcceptLicensesInvalid()
        {
            // 0. Setup
            TestLogger logger;
            string[] rawArgs;
            ProcessedArgs actualArgs;

            // 1. Correct text, wrong case -> invalid
            logger = new TestLogger();
            rawArgs = new string[] { "/a:validId", "/ACCEPTLICENSES" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 2. Unrecognized argument -> invalid
            logger = new TestLogger();
            rawArgs = new string[] { "/a:validId", "/acceptLicenses=true" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 3. Unrecognized argument -> invalid
            logger = new TestLogger();
            rawArgs = new string[] { "/a:validId", "/acceptLicensesXXX" };
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
        }

        #endregion Tests

        #region Checks

        private static void AssertArgumentsNotProcessed(ProcessedArgs actualArgs, TestLogger logger)
        {
            actualArgs.Should().BeNull("Not expecting the arguments to have been processed successfully");
            logger.AssertErrorsLogged();
        }

        private static void AssertArgumentsProcessed(ProcessedArgs actualArgs, TestLogger logger, string expectedId, string expectedVersion, string expectedSqale, bool expectedAcceptLicenses)
        {
            actualArgs.Should().NotBeNull("Expecting the arguments to have been processed successfully");

            expectedId.Should().Be(actualArgs.PackageId, "Unexpected package id returned");

            if (expectedVersion == null)
            {
                actualArgs.PackageVersion.Should().BeNull("Expecting the version to be null");
            }
            else
            {
                actualArgs.PackageVersion.Should().NotBeNull("Not expecting the version to be null");
                actualArgs.PackageVersion.ToString().Should().Be(expectedVersion);
            }

            actualArgs.SqaleFilePath.Should().Be(expectedSqale, "Unexpected sqale file path");
            if (expectedSqale != null)
            {
                File.Exists(expectedSqale).Should().BeTrue("Specified sqale file should exist: {0}", expectedSqale);
            }

            actualArgs.AcceptLicenses.Should().Be(expectedAcceptLicenses, "Unexpected value for AcceptLicenses");

            logger.AssertErrorsLogged(0);
        }

        #endregion Checks
    }
}