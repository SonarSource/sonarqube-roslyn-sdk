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

        [TestMethod]
        public void GetRoslynName_Unrecognised_Throws()
        {
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.GetRoslynLanguageName("foo"));
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.GetRoslynLanguageName("CS")); // case-sensitive
            AssertException.Expect<ArgumentOutOfRangeException>(() => SupportedLanguages.GetRoslynLanguageName("VB")); // case-sensitive
        }

        [TestMethod]
        public void GetRoslynName_Recognised_ReturnsExpected()
        {
            string result = SupportedLanguages.GetRoslynLanguageName("cs");
            Assert.AreEqual(result, "C#");

            result = SupportedLanguages.GetRoslynLanguageName("vb");
            Assert.AreEqual(result, "Visual Basic");
        }
    }
}
