/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2022 SonarSource SA
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
            string rootTestDir = TestUtils.CreateTestDirectory(TestContext);
            string originalZipFile = Path.Combine(rootTestDir, "original.zip");
            string updatedZipFile = Path.Combine(rootTestDir, "updated.zip");

            string setupDir = TestUtils.CreateTestDirectory(TestContext, ".zip.setup");
            TestUtils.CreateTextFile("file1.txt", setupDir, "file 1 content");
            TestUtils.CreateTextFile("sub1\\sub2\\file2.txt", setupDir, "file 2 content");

            ZipFile.CreateFromDirectory(setupDir, originalZipFile);

            // Sanity check that the test archive was built correctly
            using var originalChecker = new ZipFileChecker(TestContext, originalZipFile);
            originalChecker.AssertZipContainsOnlyExpectedFiles(
                // Original files
                "file1.txt",
                "sub1\\sub2\\file2.txt");

            // Create some new dummy files to add
            string addFile1 = TestUtils.CreateTextFile("additional1.txt", rootTestDir, "a1");
            string addFile2 = TestUtils.CreateTextFile("additional2.txt", rootTestDir, "a2");

            ArchiveUpdater updater = new ArchiveUpdater(new TestLogger());

            // Act
            updater.SetInputArchive(originalZipFile)
                .SetOutputArchive(updatedZipFile)
                .AddFile(addFile1, "addFile1.txt")
                .AddFile(addFile2, "sub1\\sub2\\addFile2.txt")
                .AddFile(addFile1, "newSubDir\\addFile3.txt");
            updater.UpdateArchive();

            // Assert
            using var upDatedChecker = new ZipFileChecker(TestContext, updatedZipFile);

            upDatedChecker.AssertZipContainsOnlyExpectedFiles(
                // Original files
                "file1.txt",
                "sub1\\sub2\\file2.txt",

                // Added files
                "addFile1.txt",
                "sub1\\sub2\\addFile2.txt",
                "newSubDir\\addFile3.txt"
                );
        }

        [TestMethod]
        public void ExistingEntryUpdated_And_NewEntryAdded()
        {
            // Arrange - create an input archive file
            string rootTestDir = TestUtils.CreateTestDirectory(this.TestContext);
            string originalZipFile = Path.Combine(rootTestDir, "original.zip");
            string updatedZipFile = Path.Combine(rootTestDir, "updated.zip");

            using (var archive = new ZipArchive(File.Create(originalZipFile), ZipArchiveMode.Create))
            {
                AddEntry(archive, "sub/unchanged", "unchanged value");
                AddEntry(archive, "sub/changed", "original data in file that is going to be changed to something shorted");
            }

            File.Exists(originalZipFile).Should().BeTrue("Test setup error: original zip file not created");

            string changedFile = TestUtils.CreateTextFile("changed.txt", rootTestDir, "new data in changed file");
            string newFile = TestUtils.CreateTextFile("newfile.txt", rootTestDir, "new file");


            // Act
            ArchiveUpdater updater = new ArchiveUpdater(new TestLogger());

            updater.SetInputArchive(originalZipFile)
                .SetOutputArchive(updatedZipFile)
                .AddFile(changedFile, "sub/changed")
                .AddFile(newFile, "newfile");
            updater.UpdateArchive();


            // Assert
            using (var updatedArchive = new ZipArchive(File.OpenRead(updatedZipFile), ZipArchiveMode.Read))
            {
                AssertEntryExists(updatedArchive, "sub/unchanged", "unchanged value");
                AssertEntryExists(updatedArchive, "sub/changed", "new data in changed file");
                AssertEntryExists(updatedArchive, "newfile", "new file");
            }
        }

        private static void AddEntry(ZipArchive archive, string entryFullName, string text)
        {
            var entry = archive.CreateEntry(entryFullName);
            using (var stream = entry.Open())
            {   
                var data = System.Text.Encoding.UTF8.GetBytes(text);
                stream.Write(data, 0, data.Length);
            }
        }

        private static void AssertEntryExists(ZipArchive archive, string entryFullName, string expectedText)
        {
            var entry = archive.GetEntry(entryFullName);
            entry.Should().NotBeNull($"Expected entry not found: {entryFullName}");

            const int MAX_DATA_LENGTH = 100;

            using (var stream = entry.Open())
            {
                var actualData = new byte[MAX_DATA_LENGTH];
                var actualLength = stream.Read(actualData, 0, MAX_DATA_LENGTH);
                actualLength.Should().BeLessThan(MAX_DATA_LENGTH, $"Test setup error: sample test string should be less than {MAX_DATA_LENGTH}");

                var actualText = System.Text.Encoding.UTF8.GetString(actualData, 0, actualLength);
                actualText.Should().Be(expectedText, $"Entry does not contain the expected data. Entry: {entryFullName}");
            }
        }

        #endregion
    }
}