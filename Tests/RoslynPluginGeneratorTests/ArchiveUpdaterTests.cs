//-----------------------------------------------------------------------
// <copyright file="ArchiveUpdaterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
