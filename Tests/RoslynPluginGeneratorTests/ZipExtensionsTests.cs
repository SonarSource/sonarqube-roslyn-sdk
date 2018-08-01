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

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;

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
            string testDir = TestUtils.CreateTestDirectory(TestContext);

            TestUtils.CreateTextFile("dummy.txt", testDir, "dummy content");
            TestUtils.CreateTextFile("sub1\\foo.txt", testDir, "dummy content");
            TestUtils.CreateTextFile("sub2\\bar.123", testDir, "dummy content");
            TestUtils.CreateTextFile("sub2\\archive1.zip", testDir, "dummy content");
            TestUtils.CreateTextFile("archive2.zip", testDir, "dummy content");

            // 1. Exclude zip files
            Func<string, bool> shouldInclude = f => !f.EndsWith(".zip");
            string fullzipFileName = Path.Combine(testDir, "output1.zip");

            ZipExtensions.CreateFromDirectory(testDir, fullzipFileName, shouldInclude);

            ZipFileChecker checker = new ZipFileChecker(TestContext, fullzipFileName);
            checker.AssertZipContainsOnlyExpectedFiles(
                "dummy.txt",
                "sub1\\foo.txt",
                "sub2\\bar.123");
        }

        [TestMethod]
        public void ZipDir_SimpleFilter_2()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(TestContext);

            TestUtils.CreateTextFile("dummy.txt", testDir, "dummy content");
            TestUtils.CreateTextFile("sub1\\foo.txt", testDir, "dummy content");
            TestUtils.CreateTextFile("sub2\\bar.123", testDir, "dummy content");
            TestUtils.CreateTextFile("sub2\\archive1.zip", testDir, "dummy content");
            TestUtils.CreateTextFile("archive2.zip", testDir, "dummy content");

            Func<string, bool> shouldInclude = f => f.Contains("sub");
            string fullzipFileName = Path.Combine(testDir, "output.zip");

            ZipExtensions.CreateFromDirectory(testDir, fullzipFileName, shouldInclude);

            ZipFileChecker checker = new ZipFileChecker(TestContext, fullzipFileName);
            checker.AssertZipContainsOnlyExpectedFiles(
                "sub1\\foo.txt",
                "sub2\\bar.123",
                "sub2\\archive1.zip");
        }

        #endregion Tests
    }
}