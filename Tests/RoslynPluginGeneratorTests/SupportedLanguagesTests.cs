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

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests;

[TestClass]
public class SupportedLanguagesTests
{
    [DataTestMethod]
    [DataRow("")]
    [DataRow("123")]
    [DataRow("CSharp")]
    [DataRow("CS")]
    [DataRow("Cs")]
    [DataRow("cS")]
    [DataRow("Visual Basic")]
    [DataRow("VB")]
    [DataRow("Vb")]
    [DataRow("vB")]
    [DataRow("vb.net")]
    [DataRow("vbnet")]
    public void RoslynLanguageName_Invalid_Throws(string language) =>
        ((Func<string>)(() => SupportedLanguages.RoslynLanguageName(language))).Should().Throw<ArgumentOutOfRangeException>();

    [DataTestMethod]
    [DataRow("cs", "C#")]
    [DataRow("vb", "Visual Basic")]
    public void RoslynLanguageName_Valid(string language, string expected) =>
        SupportedLanguages.RoslynLanguageName(language).Should().Be(expected);

    [DataTestMethod]
    [DataRow("")]
    [DataRow("123")]
    [DataRow("CSharp")]
    [DataRow("CS")]
    [DataRow("Cs")]
    [DataRow("cS")]
    [DataRow("Visual Basic")]
    [DataRow("VB")]
    [DataRow("Vb")]
    [DataRow("vB")]
    [DataRow("vb.net")]
    [DataRow("vbnet")]
    public void RepositoryLanguage_Invalid_Throws(string language) =>
        ((Func<string>)(() => SupportedLanguages.RepositoryLanguage(language))).Should().Throw<ArgumentOutOfRangeException>();

    [DataTestMethod]
    [DataRow("cs", "cs")]
    [DataRow("vb", "vbnet")]
    public void RepositoryLanguage_Valid(string language, string expected) =>
        SupportedLanguages.RepositoryLanguage(language).Should().Be(expected);
}