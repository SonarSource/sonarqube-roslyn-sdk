//-----------------------------------------------------------------------
// <copyright file="RepositoryKeyUtilities.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;

namespace SonarQube.Plugins
{
    /// <summary>
    /// Utility methods for creating and validating with SonarQube repository and rule keys
    /// </summary>
    public static class RepositoryKeyUtilities
    {
        private const int MaxKeyLength = 255;

        /// <summary>
        /// Allowed a limited set of symbol characters
        /// </summary>
        private static char[] allowedSymbols = new char[] { '.', '-', '_' };

        public static string GetValidKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            StringBuilder sb = new StringBuilder();

            foreach (char c in key)
            {
                if (IsValidChar(c))
                {
                    sb.Append(c);
                }
            }

            string retval = sb.ToString().ToLowerInvariant(); // lower-case by convention
            if (retval.Length > MaxKeyLength)
            {
                retval = retval.Substring(0, MaxKeyLength);
            }

            ThrowIfInvalid(retval);
            return retval;
        }

        public static void ThrowIfInvalid(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || 
                key.Length > 255 ||
                key.Any(c => !IsValidChar(c)))
            {
                throw new ArgumentException(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture, UIResources.Misc_Error_InvalidRepositoryKey, key), "key");
            }
        }

        private static bool IsValidChar(char c)
        {
            bool isValid = char.IsLetterOrDigit(c) || allowedSymbols.Any(a => a == c);
            return isValid;
        }

    }
}
