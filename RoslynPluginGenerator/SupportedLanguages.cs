//-----------------------------------------------------------------------
// <copyright file="SupportedLanguages.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// List of languages supported by the Roslyn plugin generator
    /// </summary>
    /// <remarks>The values of the constants are the language ids used by SonarQube</remarks>
    public static class SupportedLanguages
    {
        public const string CSharp = "cs";
        public const string VisualBasic = "vb";

        public static bool IsSupported(string language)
        {
            bool supported = string.Equals(language, CSharp, System.StringComparison.Ordinal) ||
                string.Equals(language, VisualBasic, System.StringComparison.Ordinal);

            return supported;
        }

        public static string GetRoslynLanguageName(string language)
        {
            ThrowIfNotSupported(language);

            if (string.Equals(language, CSharp, System.StringComparison.OrdinalIgnoreCase))
            {
                return Microsoft.CodeAnalysis.LanguageNames.CSharp;
            }
            return Microsoft.CodeAnalysis.LanguageNames.VisualBasic;
        }

        public static void ThrowIfNotSupported(string language)
        {
            if (!SupportedLanguages.IsSupported(language))
            {
                throw new System.ArgumentOutOfRangeException(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    UIResources.APG_UnsupportedLanguage, language), "language");
            }
        }
    }
}
