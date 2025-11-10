/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource Sàrl
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

        [TestMethod]
        public void ArgProc_EmptyArgs()
        {
            TestLogger logger = new TestLogger();
            string[] rawArgs = [];

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
            rawArgs = ["/analyzer:"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 2. Id and missing version
            logger = new TestLogger();
            rawArgs = ["/analyzer:testing.id.missing.version:"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 3. Id and invalid version
            logger = new TestLogger();
            rawArgs = ["/analyzer:testing.id.invalid.version:1.0.-invalid.version.1"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 4. Missing id
            logger = new TestLogger();
            rawArgs = ["/analyzer::2.1.0"];
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
            rawArgs = ["/a:testing.id.no.version"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "testing.id.no.version", null, false);

            // 2. Id and version
            logger = new TestLogger();
            rawArgs = ["/analyzer:testing.id.with.version:1.0.0-rc1"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "testing.id.with.version", "1.0.0-rc1", false);

            // 3. Id containing a colon, with version
            logger = new TestLogger();
            rawArgs = ["/analyzer:id.with:colon:2.1.0"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "id.with:colon", "2.1.0", false);
        }

        [TestMethod]
        public void ArgProc_SqaleParameterIsDeprecated()
        {
            TestLogger logger = new TestLogger();
            string[] rawArgs = ["/sqale:mySqaleFile.txt", "/a:validId"];

            ProcessedArgs actualArgs;
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
            logger.AssertSingleErrorExists("The /sqale parameter is no longer supported");
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
            rawArgs = ["/a:validId"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            actualArgs.RuleFilePath.Should().BeNull();

            // 2. Missing rule file
            logger = new TestLogger();
            rawArgs = ["/rules:missingFile.txt", "/a:validId"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
            logger.AssertSingleErrorExists("missingFile.txt"); // should be an error containing the missing file name

            // 3. Existing rule file
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = TestUtils.CreateTextFile("valid.rules.txt", testDir, "rule file contents");

            logger = new TestLogger();
            rawArgs = [$"/rules:{filePath}", "/a:valid:1.0"];
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
            rawArgs = ["/a:validId", "/acceptLicenses"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsProcessed(actualArgs, logger, "validId", null, true);
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
            rawArgs = ["/a:validId", "/ACCEPTLICENSES"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 2. Unrecognized argument -> invalid
            logger = new TestLogger();
            rawArgs = ["/a:validId", "/acceptLicenses=true"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);

            // 3. Unrecognized argument -> invalid
            logger = new TestLogger();
            rawArgs = ["/a:validId", "/acceptLicensesXXX"];
            actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
        }

        [TestMethod]
        public void ArgProc_OutputDirectory_Parameter()
        {
            var logger = new TestLogger();
            var rawArgs = new string[] { "/a:validId", "/o:My/Output/Directory" };
            var actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            actualArgs.OutputDirectory.Should().Be("My/Output/Directory");
        }

        [TestMethod]
        public void ArgProc_OutputDirectory_Default()
        {
            var logger = new TestLogger();
            var rawArgs = new string[] { "/a:validId" };
            var actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            actualArgs.OutputDirectory.Should().Be(Directory.GetCurrentDirectory());
        }

        [TestMethod]
        public void ArgProc_CustomNuGetRepository_Default()
        {
            var logger = new TestLogger();
            var rawArgs = new string[] { "/a:validId" };
            var actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            actualArgs.CustomNuGetRepository.Should().BeNull();
        }

        [TestMethod]
        public void ArgProc_CustomNuGetRepository_Path()
        {
            var logger = new TestLogger();
            var rawArgs = new string[] { "/a:validId", "/customnugetrepo:file:///somelocalrepo/path" };
            var actualArgs = ArgumentProcessor.TryProcessArguments(rawArgs, logger);

            actualArgs.CustomNuGetRepository.Should().Be("file:///somelocalrepo/path");
        }

        [TestMethod]
        public void ArgProc_Language_Default()
        {
            var actualArgs = ArgumentProcessor.TryProcessArguments(["/a:irrelevant"], new TestLogger());
            actualArgs.Language.Should().Be("cs");
        }

        [TestMethod]
        public void ArgProc_Language_Valid()
        {
            var actualArgs = ArgumentProcessor.TryProcessArguments(["/a:valid", "/language:cs"], new TestLogger());
            actualArgs.Language.Should().Be("cs");

            actualArgs = ArgumentProcessor.TryProcessArguments(["/a:valid", "/language:vb"], new TestLogger());
            actualArgs.Language.Should().Be("vb");
        }

        [TestMethod]
        public void ArgProc_Language_Invalid()
        {
            var logger = new TestLogger();
            var actualArgs = ArgumentProcessor.TryProcessArguments(["/a:valid", "/language:invalid"], logger);

            AssertArgumentsNotProcessed(actualArgs, logger);
            logger.AssertErrorLogged("Invalid language parameter: invalid");
        }

        private static void AssertArgumentsNotProcessed(ProcessedArgs actualArgs, TestLogger logger)
        {
            actualArgs.Should().BeNull("Not expecting the arguments to have been processed successfully");
            logger.AssertErrorsLogged();
        }

        private static void AssertArgumentsProcessed(ProcessedArgs actualArgs, TestLogger logger, string expectedId, string expectedVersion, bool expectedAcceptLicenses)
        {
            actualArgs.Should().NotBeNull("Expecting the arguments to have been processed successfully");
            expectedId.Should().Be(actualArgs.PackageId, "Unexpected package id returned");

            if (expectedVersion is null)
            {
                actualArgs.PackageVersion.Should().BeNull("Expecting the version to be null");
            }
            else
            {
                actualArgs.PackageVersion.Should().NotBeNull("Not expecting the version to be null");
                actualArgs.PackageVersion.ToString().Should().Be(expectedVersion);
            }
            actualArgs.AcceptLicenses.Should().Be(expectedAcceptLicenses, "Unexpected value for AcceptLicenses");
            logger.AssertErrorsLogged(0);
        }
    }
}