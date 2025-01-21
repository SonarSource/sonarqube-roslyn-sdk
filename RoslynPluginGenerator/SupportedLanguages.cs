/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource SA
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

namespace SonarQube.Plugins.Roslyn;

/// <summary>
/// List of languages supported by the Roslyn plugin generator
/// </summary>
/// <remarks>The values of the constants are the language ids used by SonarQube</remarks>
public static class SupportedLanguages
{
    public const string CSharp = "cs";
    public const string VisualBasic = "vb";

    public static bool IsSupported(string language) =>
        string.Equals(language, CSharp, StringComparison.Ordinal) || string.Equals(language, VisualBasic, StringComparison.Ordinal);

    public static string RoslynLanguageName(string language) =>
        language switch
        {
            CSharp => Microsoft.CodeAnalysis.LanguageNames.CSharp,
            VisualBasic => Microsoft.CodeAnalysis.LanguageNames.VisualBasic,
            _ => throw UnsupportedException(language)
        };

    public static string RepositoryLanguage(string language) =>
        language switch
        {
            CSharp => "cs",
            VisualBasic => "vbnet",
            _ => throw UnsupportedException(language)
        };

    private static ArgumentOutOfRangeException UnsupportedException(string language) =>
        new(nameof(language), string.Format(System.Globalization.CultureInfo.CurrentCulture, UIResources.APG_UnsupportedLanguage, language));
}