//-----------------------------------------------------------------------
// <copyright file="JarChecker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.IO.Compression;
using Tests.Common;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    internal class JarChecker
    {
        private readonly string unzippedDir;

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
