//-----------------------------------------------------------------------
// <copyright file="PluginKeyUtilitiesTests.cs" company="SonarSource SA and Microsoft Corporation">
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
    public class PluginKeyUtilitiesTests
    {
        #region Tests

        [TestMethod]
        public void PluginKey_InputCanBeCorrected_ValidKeyReturned()
        {
            // Already valid
            TestGetValidKey("1234567890abc", "1234567890abc");
            TestGetValidKey("a1b2c3", "a1b2c3");

            // Spaces stripped
            TestGetValidKey(" aaa bbb ccc 111 ", "aaabbbccc111");

            // Non-alpha stripped
            TestGetValidKey("foo.analyzers", "fooanalyzers");
            TestGetValidKey("x!\"£$%^&*()_+", "x");

            // Case-changed
            TestGetValidKey("Bar.Analyzers", "baranalyzers");
            TestGetValidKey("XxX", "xxx");
        }

        [TestMethod]
        public void PluginKey_InputCannotBeCorrected_Throws()
        {
            CheckGetValidKeyThrows(null);
            CheckGetValidKeyThrows("");
            CheckGetValidKeyThrows("....");
            CheckGetValidKeyThrows("~@{}");
        }

        [TestMethod]
        public void PluginKey_ThrowsOnInvalid()
        {
            CheckThrowIfInvalidThrows(null);
            CheckThrowIfInvalidThrows("");

            CheckThrowIfInvalidThrows("Foo.Analyzers");
            CheckThrowIfInvalidThrows(" aaa bbb ccc 111 ");
            CheckThrowIfInvalidThrows("x!\"£$%^&*()_+");
        }

        #endregion

        #region Private methods

        private static void TestGetValidKey(string input, string expected)
        {
            string actual = PluginKeyUtilities.GetValidKey(input);
            Assert.AreEqual(expected, actual, "Unexpected plugin key returned");

            PluginKeyUtilities.ThrowIfInvalid(expected); // should not throw on values returned by GetValidKey
        }

        private static void CheckGetValidKeyThrows(string input)
        {
            // Should throw on input that cannot be corrected
            AssertException.Expect<ArgumentException>(() => PluginKeyUtilities.GetValidKey(input));
        }

        private static void CheckThrowIfInvalidThrows(string input)
        {
            AssertException.Expect<ArgumentException>(() => PluginKeyUtilities.ThrowIfInvalid(input));
        }

        #endregion

    }
}
