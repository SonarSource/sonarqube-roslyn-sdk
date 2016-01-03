//-----------------------------------------------------------------------
// <copyright file="RoslynGenTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Common;
using SonarQube.Plugins.Test.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarQube.Plugins.IntegrationTests
{
    [TestClass]
    public class RoslynGenTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod] [Ignore] // WIP
        public void RoslynGen()
        {
            JdkWrapper jdkWrapper = new JdkWrapper();
            
            if (!jdkWrapper.IsJdkInstalled())
            {
                Assert.Inconclusive("Test requires the JDK to be installed");
            }

            TestLogger logger = new TestLogger();

            // Build the Java inspector class
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string srcDir = TestUtils.CreateTestDirectory(this.TestContext, "src");
            SourceGenerator.CreateSourceFiles(this.GetType().Assembly,
                "SonarQube.Plugins.IntegrationTests.Roslyn.Resources",
                srcDir,
                new Dictionary<string, string>());

            // TODO: add required jar files

            string[] srcFiles = Directory.GetFiles(srcDir, "*.java", SearchOption.AllDirectories);
            bool result = jdkWrapper.CompileSources(srcFiles, logger);
            if (result)
            {
                Assert.Inconclusive("Test setup error: failed to build the Java inspector");
            }
            
            // Build the exe
            string exePath = typeof(Roslyn.AnalyzerPluginGenerator).Assembly.Location;
            ProcessRunner runner = new ProcessRunner();

            ProcessRunnerArguments args = new ProcessRunnerArguments(exePath, logger);

            args.CmdLineArgs = new string[] { "/a:Wintellect.Analyzers:1.0.5" };
            args.WorkingDirectory = Path.GetDirectoryName(exePath);

            result = runner.Execute(args);
        }
    }
}
