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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Plugins.Test.Common
{
    public static class TestUtils
    {
        public static string CreateTestDirectory(TestContext testContext, params string[] subDirs)
        {
            string fullPath = GetTestDirectoryName(testContext, subDirs);
            Assert.IsFalse(Directory.Exists(fullPath), "Test directory should not already exist: {0}", fullPath);
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
                Assert.IsTrue(Path.IsPathRooted(fileName), "Test error: expecting the supplied file path to be absolute. File: {0}", fileName);
                fullPath = fileName;
            }
            else
            {
                fullPath = Path.Combine(parentDirectory, fileName);
            }
            Assert.IsTrue(File.Exists(fullPath), "Expected file does not exist: {0}", fullPath);

            return File.ReadAllText(fullPath);
        }

        public static void AssertFileDoesNotExist(string fileName, string parentDirectory = null)
        {
            string fullPath;
            if (parentDirectory == null)
            {
                Assert.IsTrue(Path.IsPathRooted(fileName), "Test error: expecting the supplied file path to be absolute. File: {0}", fileName);
                fullPath = fileName;
            }
            else
            {
                fullPath = Path.Combine(parentDirectory, fileName);
            }

            Assert.IsFalse(File.Exists(fullPath), "Not expecting file to exist: {0}", fullPath);
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

        /// <summary>
        /// Executes the action and fails the test if the expected exception is not thrown.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static void AssertExceptionIsThrown<TException>(Action action)
             where TException : Exception
        {
            try
            {
                action();
            }
            catch (Exception thrownException)
            {
                if (thrownException is TException)
                {
                    return;
                }
                Assert.Fail("Exception of type " + typeof(TException).FullName + " was expected, but got " + thrownException.GetType().FullName + " instead.");
            }

            Assert.Fail("Exception of type " + typeof(TException).FullName + " was expected, but was not thrown");
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