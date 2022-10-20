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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class SupportedLanguagesTests
    {
        [TestMethod]
        public void ThrowIfNotSupported_Unrecognised_Throws()
        {
            Action action = () => SupportedLanguages.ThrowIfNotSupported("");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();

            action = () => SupportedLanguages.ThrowIfNotSupported("123");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();

            action = () => SupportedLanguages.ThrowIfNotSupported("Visual Basic");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();

            action = () => SupportedLanguages.ThrowIfNotSupported("CSharp");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();

            action = () => SupportedLanguages.ThrowIfNotSupported("Cs");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();

            action = () => SupportedLanguages.ThrowIfNotSupported("CS");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();

            action = () => SupportedLanguages.ThrowIfNotSupported("vB");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();

            action = () => SupportedLanguages.ThrowIfNotSupported("VB");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();
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
            Action action = () => SupportedLanguages.GetRoslynLanguageName("foo");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>();

            action = () => SupportedLanguages.GetRoslynLanguageName("CS");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>(); // case-sensitive

            action = () => SupportedLanguages.GetRoslynLanguageName("VB");
            action.Should().ThrowExactly<ArgumentOutOfRangeException>(); // case-sensitive
        }

        [TestMethod]
        public void GetRoslynName_Recognised_ReturnsExpected()
        {
            string result = SupportedLanguages.GetRoslynLanguageName("cs");
            result.Should().Be("C#");

            result = SupportedLanguages.GetRoslynLanguageName("vb");
            result.Should().Be("Visual Basic");
        }
    }
}