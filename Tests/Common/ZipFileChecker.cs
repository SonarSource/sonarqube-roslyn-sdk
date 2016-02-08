//-----------------------------------------------------------------------
// <copyright file="ZipFileChecker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.IO.Compression;

namespace SonarQube.Plugins.Test.Common
{
    /// <summary>
    /// Utility class used to check the file content of zipped files, which could be jar files
    /// </summary>
    public class ZipFileChecker
    {
        private readonly string unzippedDir;
        private readonly TestContext testContext;

        public ZipFileChecker(TestContext testContext, string zipFilePath)
        {
            this.testContext = testContext;
            TestUtils.AssertFileExists(zipFilePath);

            this.unzippedDir = TestUtils.CreateTestDirectory(testContext, "unzipped." + Path.GetFileNameWithoutExtension(zipFilePath));
            ZipFile.ExtractToDirectory(zipFilePath, this.unzippedDir);
        }

        /// <summary>
        /// Returns the folder into which the zip was unpacked
        /// </summary>
        public string UnzippedDirectoryPath {  get { return this.unzippedDir; } }

        public void AssertZipContainsFiles(params string[] expectedRelativePaths)
        {
            foreach (string relativePath in expectedRelativePaths)
            {
                this.testContext.WriteLine("ZipFileChecker: checking for file '{0}'", relativePath);

                string[] matchingFiles = Directory.GetFiles(this.unzippedDir, relativePath, SearchOption.TopDirectoryOnly);

                Assert.IsTrue(matchingFiles.Length < 2, "Test error: supplied relative path should not match multiple files");
                Assert.AreEqual(1, matchingFiles.Length, "Zip file does not contain expected file: {0}", relativePath);

                this.testContext.WriteLine("ZipFileChecker: found at '{0}'", matchingFiles[0]);
            }
        }

    }
}
