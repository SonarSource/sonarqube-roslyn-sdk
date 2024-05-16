/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2024 SonarSource SA
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Plugins.Test.Common
{
    public static class TestUtils
    {
        public static string CreateTestDirectory(TestContext testContext, params string[] subDirs)
        {
            string fullPath = GetTestDirectoryName(testContext, subDirs);
            Directory.Exists(fullPath).Should().BeFalse("Test directory should not already exist: {0}", fullPath);
            Directory.CreateDirectory(fullPath);

            testContext.WriteLine("Test setup: created directory: {0}", fullPath);

            return fullPath;
        }

        public static string EnsureTestDirectoryExists(TestContext testContext, params string[] subDirs)
        {
            string fullPath = GetTestDirectoryName(testContext, subDirs);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);

                testContext.WriteLine("Test setup: created directory: {0}", fullPath);
            }
            return fullPath;
        }

        /// <summary>
        /// Checks the file exists and return the contents
        /// </summary>
        public static string AssertFileExists(string fileName, string parentDirectory = null)
        {
            string fullPath;
            if (parentDirectory == null)
            {
                Path.IsPathRooted(fileName).Should().BeTrue("Test error: expecting the supplied file path to be absolute. File: {0}", fileName);
                fullPath = fileName;
            }
            else
            {
                fullPath = Path.Combine(parentDirectory, fileName);
            }
            File.Exists(fullPath).Should().BeTrue("Expected file does not exist: {0}", fullPath);

            return File.ReadAllText(fullPath);
        }

        public static void AssertFileDoesNotExist(string fileName, string parentDirectory = null)
        {
            string fullPath;
            if (parentDirectory == null)
            {
                Path.IsPathRooted(fileName).Should().BeTrue("Test error: expecting the supplied file path to be absolute. File: {0}", fileName);
                fullPath = fileName;
            }
            else
            {
                fullPath = Path.Combine(parentDirectory, fileName);
            }

            File.Exists(fullPath).Should().BeFalse("Not expecting file to exist: {0}", fullPath);
        }

        public static string CreateTextFile(string relativeFileName, string directory, string content = null)
        {
            string fullPath = Path.Combine(directory, relativeFileName);

            // Ensure the directory exists
            string fullDirectory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(fullDirectory);

            File.WriteAllText(fullPath, content ?? string.Empty);
            return fullPath;
        }

        #region Private methods

        private static string GetTestDirectoryName(TestContext testContext, params string[] subDirs)
        {
            List<string> parts = new List<string>
            {
                testContext.TestDeploymentDir,
                testContext.TestName
            };

            if (subDirs.Any())
            {
                parts.AddRange(subDirs);
            }

            string fullPath = Path.Combine(parts.ToArray());
            return fullPath;
        }

        #endregion Private methods
    }
}