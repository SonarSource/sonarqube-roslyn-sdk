//-----------------------------------------------------------------------
// <copyright file="ZipExtensionsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System;
using System.IO;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class ZipExtensionsTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void ZipDir_SimpleFilter_1()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);

            TestUtils.CreateTextFile("dummy.txt", testDir, "dummy content");
            TestUtils.CreateTextFile("sub1\\foo.txt", testDir, "dummy content");
            TestUtils.CreateTextFile("sub2\\bar.123", testDir, "dummy content");
            TestUtils.CreateTextFile("sub2\\archive1.zip", testDir, "dummy content");
            TestUtils.CreateTextFile("archive2.zip", testDir, "dummy content");

            // 1. Exclude zip files
            Func<string, bool> shouldInclude = f => !f.EndsWith(".zip");
            string fullzipFileName = Path.Combine(testDir, "output1.zip");

            ZipExtensions.CreateFromDirectory(testDir, fullzipFileName, shouldInclude);

            ZipFileChecker checker = new ZipFileChecker(this.TestContext, fullzipFileName);
            checker.AssertZipContainsOnlyExpectedFiles(
                "dummy.txt",
                "sub1\\foo.txt",
                "sub2\\bar.123");
        }

        [TestMethod]
        public void ZipDir_SimpleFilter_2()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);

            TestUtils.CreateTextFile("dummy.txt", testDir, "dummy content");
            TestUtils.CreateTextFile("sub1\\foo.txt", testDir, "dummy content");
            TestUtils.CreateTextFile("sub2\\bar.123", testDir, "dummy content");
            TestUtils.CreateTextFile("sub2\\archive1.zip", testDir, "dummy content");
            TestUtils.CreateTextFile("archive2.zip", testDir, "dummy content");

            Func<string, bool> shouldInclude = f => f.Contains("sub");
            string fullzipFileName = Path.Combine(testDir, "output.zip");

            ZipExtensions.CreateFromDirectory(testDir, fullzipFileName, shouldInclude);

            ZipFileChecker checker = new ZipFileChecker(this.TestContext, fullzipFileName);
            checker.AssertZipContainsOnlyExpectedFiles(
                "sub1\\foo.txt",
                "sub2\\bar.123",
                "sub2\\archive1.zip");
        }

        #endregion

    }
}
