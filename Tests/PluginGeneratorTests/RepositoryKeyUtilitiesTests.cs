//-----------------------------------------------------------------------
// <copyright file="RepositoryKeyUtilitiesTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System;

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

        #endregion

        #region Private methods

        private static void TestGetValidKey(string input, string expected)
        {
            string actual = RepositoryKeyUtilities.GetValidKey(input);
            Assert.AreEqual(expected, actual, "Unexpected key returned");

            RepositoryKeyUtilities.ThrowIfInvalid(expected); // should not throw on values returned by GetValidKey
        }

        private static void CheckGetValidKeyThrows(string input)
        {
            // Should throw on input that cannot be corrected
            AssertException.Expect<ArgumentException>(() => RepositoryKeyUtilities.GetValidKey(input));
        }

        private static void CheckThrowIfInvalidThrows(string input)
        {
            AssertException.Expect<ArgumentException>(() => RepositoryKeyUtilities.ThrowIfInvalid(input));
        }

        #endregion

    }
}
