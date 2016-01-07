//-----------------------------------------------------------------------
// <copyright file="JarChecker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;
using System.IO.Compression;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    internal class JarChecker
    {
        private readonly string unzippedDir;
        private readonly TestContext testContext;

        public JarChecker(TestContext testContext, string fullJarPath)
        {
            this.testContext = testContext;
            TestUtils.AssertFileExists(fullJarPath);

            this.unzippedDir = TestUtils.CreateTestDirectory(testContext, "unzipped");
            ZipFile.ExtractToDirectory(fullJarPath, this.unzippedDir);
        }

        public void JarContainsFiles(params string[] expectedRelativePaths)
        {
            foreach (string relativePath in expectedRelativePaths)
            {
                this.testContext.WriteLine("JarChecker: checking for file '{0}'", relativePath);

                string[] matchingFiles = Directory.GetFiles(this.unzippedDir, relativePath, SearchOption.TopDirectoryOnly);

                Assert.IsTrue(matchingFiles.Length < 2, "Test error: supplied relative path should not match multiple files");
                Assert.AreEqual(1, matchingFiles.Length, "Jar does not contain expected file: {0}", relativePath);

                this.testContext.WriteLine("JarChecker: found at '{0}'", matchingFiles[0]);
            }
        }

    }
}
