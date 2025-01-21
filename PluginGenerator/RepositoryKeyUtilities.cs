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
                throw new ArgumentNullException(nameof(key));
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