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
    }
}
