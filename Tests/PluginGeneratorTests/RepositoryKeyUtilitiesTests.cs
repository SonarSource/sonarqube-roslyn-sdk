/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource Sàrl
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class RepositoryKeyUtilitiesTests
    {
        #region Tests

        [TestMethod]
        public void RepoKey_InputCanBeCorrected_ValidKeyReturned()
        {
            // Already valid
            TestGetValidKey("1234567890abc", "1234567890abc");
            TestGetValidKey("a1b2c3", "a1b2c3");

            // Dots allowed
            TestGetValidKey("aaa.bbb.ccc.111", "aaa.bbb.ccc.111");
            TestGetValidKey("...", "...");
            TestGetValidKey(".-_", ".-_");

            // Spaces stripped
            TestGetValidKey(" aaa bbb ccc 111 ", "aaabbbccc111");

            // Disallowed symbols stripped
            TestGetValidKey("x-_.!\"£$%^&*()+", "x-_.");

            // Case-changed
            TestGetValidKey("Bar.Analyzers", "bar.analyzers");
            TestGetValidKey("XxX", "xxx");

            // Too long -> truncated
            string inputLongString = new string('a', 300);
            string expectedLongString = new string('a', 255);
            TestGetValidKey(inputLongString, expectedLongString);

            TestGetValidKey("!!!" + expectedLongString + "   ", expectedLongString);
        }

        [TestMethod]
        public void RepoKey_InputCannotBeCorrected_Throws()
        {
            CheckGetValidKeyThrows(null);
            CheckGetValidKeyThrows("");
            CheckGetValidKeyThrows("~@{}");
        }

        [TestMethod]
        public void RepoKey_ThrowsOnInvalid()
        {
            CheckThrowIfInvalidThrows(null);
            CheckThrowIfInvalidThrows("");

            CheckThrowIfInvalidThrows(" aaa bbb ccc 111 ");
            CheckThrowIfInvalidThrows("x!\"£$%^&*()_+");

            // Too long
            string longString = new string('a', 256);
            CheckThrowIfInvalidThrows(longString);
        }

        #endregion Tests

        #region Private methods

        private static void TestGetValidKey(string input, string expected)
        {
            string actual = RepositoryKeyUtilities.GetValidKey(input);
            actual.Should().Be(expected, "Unexpected key returned");

            RepositoryKeyUtilities.ThrowIfInvalid(expected); // should not throw on values returned by GetValidKey
        }

        private static void CheckGetValidKeyThrows(string input)
        {
            // Should throw on input that cannot be corrected
            Action action = () => RepositoryKeyUtilities.GetValidKey(input);
            action.Should().Throw<ArgumentException>();
        }

        private static void CheckThrowIfInvalidThrows(string input)
        {
            Action action = () => RepositoryKeyUtilities.ThrowIfInvalid(input);
            action.Should().ThrowExactly<ArgumentException>();
        }

        #endregion Private methods
    }
}