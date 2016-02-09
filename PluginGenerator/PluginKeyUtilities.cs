//-----------------------------------------------------------------------
// <copyright file="PluginBuilder.cs" company="SonarSource SA and Microsoft Corporation">
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
