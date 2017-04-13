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

using System;
using System.Linq;
using System.Text;

namespace SonarQube.Plugins
{
    /// <summary>
    /// Utility methods for creating and validating with SonarQube plugin keys
    /// </summary>
    public static class PluginKeyUtilities
    {
        /// <summary>
        /// Only letters and digits are allowed in plugin keys:
        /// see https://github.com/SonarSource/sonar-packaging-maven-plugin/blob/master/src/main/java/org/sonarsource/pluginpackaging/PluginKeyUtils.java#L44
        /// </summary>
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
            string retVal = sb.ToString().ToLowerInvariant(); // lower-case by convention

            // Throw if it wasn't possible to create a valid key
            ThrowIfInvalid(retVal);
            return retVal;
        }
        
        public static void ThrowIfInvalid(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Any(c => !IsValidChar(c)))
            {
                throw new ArgumentException(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture, UIResources.Misc_Error_InvalidPluginKey, key), "key");
            }
        }

        private static bool IsValidChar(char c)
        {
            return char.IsLetterOrDigit(c);
        }

    }
}
