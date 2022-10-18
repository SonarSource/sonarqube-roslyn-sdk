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

using System.IO;
using System.IO.Compression;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Plugins.Test.Common
{
    /// <summary>
    /// Utility class used to check the file content of zipped files, which could be jar files
    /// </summary>
    public class ZipFileChecker
    {
        private readonly TestContext testContext;

        public ZipFileChecker(TestContext testContext, string zipFilePath)
        {
            this.testContext = testContext;
            TestUtils.AssertFileExists(zipFilePath);
            testContext.AddResultFile(zipFilePath);

            UnzippedDirectoryPath = TestUtils.CreateTestDirectory(testContext, "unzipped." + Path.GetFileNameWithoutExtension(zipFilePath));
            ZipFile.ExtractToDirectory(zipFilePath, UnzippedDirectoryPath);

            DumpZipFile(zipFilePath);
        }

        /// <summary>
        /// Returns the folder into which the zip was unpacked
        /// </summary>
        public string UnzippedDirectoryPath { get; }

        public string AssertFileExists(string relativeFilePath)
        {
            string absolutePath = Path.Combine(UnzippedDirectoryPath, relativeFilePath);
            File.Exists(absolutePath).Should().BeTrue($"File does not exist in the zip: Relative path: {relativeFilePath}, absolute path: {absolutePath}");
            return absolutePath;
        }

        public void AssertZipContainsFiles(params string[] expectedRelativePaths)
        {
            foreach (string relativePath in expectedRelativePaths)
            {
                testContext.WriteLine("ZipFileChecker: checking for file '{0}'", relativePath);

                string[] matchingFiles = Directory.GetFiles(UnzippedDirectoryPath, relativePath, SearchOption.TopDirectoryOnly);

                matchingFiles.Length.Should().BeLessThan(2, $"Test error: supplied relative path should not match multiple files: {relativePath}, count: {matchingFiles.Length}");
                matchingFiles.Length.Should().Be(1, $"Zip file does not contain expected file: {relativePath}");

                testContext.WriteLine("ZipFileChecker: found at '{0}'", matchingFiles[0]);
            }
        }

        public void AssertZipDoesNotContainFiles(params string[] unexpectedRelativePaths)
        {
            foreach (string relativePath in unexpectedRelativePaths)
            {
                testContext.WriteLine("ZipFileChecker: checking for file '{0}'", relativePath);

                string[] matchingFiles = Directory.GetFiles(UnzippedDirectoryPath, relativePath, SearchOption.TopDirectoryOnly);

                matchingFiles.Length.Should().Be(0, $"Zip file contains unexpected file: {relativePath}");
            }
        }

        public void AssertZipContainsOnlyExpectedFiles(params string[] expectedRelativePaths)
        {
            AssertZipContainsFiles(expectedRelativePaths);

            string[] allFilesInZip = Directory.GetFiles(UnzippedDirectoryPath, "*.*", SearchOption.AllDirectories);
            allFilesInZip.Length.Should().Be(expectedRelativePaths.Length, "Zip contains more files than expected");
        }

        private void DumpZipFile(string zipFilePath)	
        {	
            // Dump the zip contents	
            var filesInZip = Directory.GetFiles(UnzippedDirectoryPath, "*.*", SearchOption.AllDirectories);	
            testContext.WriteLine($"Zip file contents: {zipFilePath}");	
            testContext.WriteLine($"File count: {filesInZip.Length}");	
            foreach (string file in filesInZip)	
            {	
                testContext.WriteLine($"  {file}");	
            }	
        }
    }
}