//-----------------------------------------------------------------------
// <copyright file="SupportedLanguagesTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class SupportedLanguagesTests
    {
        [TestMethod]
        public void ThrowIfNotSupported_Unrecognised_Throws()
        {
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.ThrowIfNotSupported(""));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.ThrowIfNotSupported("123"));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.ThrowIfNotSupported("Visual Basic"));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.ThrowIfNotSupported("CSharp"));

            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.ThrowIfNotSupported("Cs"));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.ThrowIfNotSupported("CS"));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.ThrowIfNotSupported("vB"));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.ThrowIfNotSupported("VB"));
        }

        [TestMethod]
        public void ThrowIfNotSupported_Recognised_DoesNotThrow()
        {
            SupportedLanguages.ThrowIfNotSupported("vb");
            SupportedLanguages.ThrowIfNotSupported("cs");
        }

        public void GetRoslynName_Unrecognised_Throws()
        {
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.GetRoslynLanguageName("foo"));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.GetRoslynLanguageName("CS"));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.GetRoslynLanguageName("VB"));
        }

        public void GetRoslynName_Recognised_ReturnsExpected()
        {
            string result = SupportedLanguages.GetRoslynLanguageName("cs");
            Assert.AreEqual(result, "C#");

            result = SupportedLanguages.GetRoslynLanguageName("VB");
            Assert.AreEqual(result, "Visual Basic");

            result = SupportedLanguages.GetRoslynLanguageName("cS");
        }
    }
}
