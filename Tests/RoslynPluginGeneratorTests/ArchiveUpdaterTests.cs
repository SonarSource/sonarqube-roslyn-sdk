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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;
using System.IO.Compression;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class ArchiveUpdaterTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void ArchiveUpdater_SimpleUpdateJar_Succeeds()
        {
            // Arrange - create an input archive file
            string rootTestDir = TestUtils.CreateTestDirectory(this.TestContext);
            string originalZipFile = Path.Combine(rootTestDir, "original.zip");
            string updatedZipFile = Path.Combine(rootTestDir, "updated.zip");

            string setupDir = TestUtils.CreateTestDirectory(this.TestContext, ".zip.setup");
            TestUtils.CreateTextFile("file1.txt", setupDir, "file 1 content");
            TestUtils.CreateTextFile("sub1\\sub2\\file2.txt", setupDir, "file 2 content");

            ZipFile.CreateFromDirectory(setupDir, originalZipFile);

            // Sanity check that the test archive was built correctly
            ZipFileChecker checker = new ZipFileChecker(this.TestContext, originalZipFile);
            checker.AssertZipContainsOnlyExpectedFiles(
                // Original files
                "file1.txt",
                "sub1\\sub2\\file2.txt");


            // Create some new dummy files to add
            string addFile1 = TestUtils.CreateTextFile("additional1.txt", rootTestDir, "a1");
            string addFile2 = TestUtils.CreateTextFile("additional2.txt", rootTestDir, "a2");

            string updaterRootDir = TestUtils.CreateTestDirectory(this.TestContext, "updater");

            ArchiveUpdater updater = new ArchiveUpdater(updaterRootDir, new TestLogger());

            // Act
            updater.SetInputArchive(originalZipFile)
                .SetOutputArchive(updatedZipFile)
                .AddFile(addFile1, "addFile1.txt")
                .AddFile(addFile2, "sub1\\sub2\\addFile2.txt")
                .AddFile(addFile1, "newSubDir\\addFile3.txt");
            updater.UpdateArchive();

            // Assert
            checker = new ZipFileChecker(this.TestContext, updatedZipFile);

            checker.AssertZipContainsOnlyExpectedFiles(
                // Original files
                "file1.txt",
                "sub1\\sub2\\file2.txt",

                // Added files
                "addFile1.txt",
                "sub1\\sub2\\addFile2.txt",
                "newSubDir\\addFile3.txt"
                );
        }

        #endregion
    }
}
