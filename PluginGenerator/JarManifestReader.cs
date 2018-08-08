/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2018 SonarSource SA
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
using System.Collections.Generic;

namespace SonarQube.Plugins
{
    /// <summary>
    /// Reads a valid v1.0 Jar Manifest.
    /// See http://docs.oracle.com/javase/6/docs/technotes/guides/jar/jar.html#JAR%20Manifest
    /// </summary>
    public class JarManifestReader
    {
        private readonly Dictionary<string, string> kvps;
        private const string SEPARATOR = ": ";

        public JarManifestReader(string manifestText)
        {
            if (manifestText == null)
            {
                throw new ArgumentNullException(nameof(manifestText));
            }

            kvps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // A manifest file line can have at most 72 characters. Long values are split across
            // multiple lines, with the continuation line starting with a single space.
            // The simplest way to rejoin the lines is just to replace all (EOL + space) with EOL
            var joinedText = manifestText.Replace("\r\n ", string.Empty);
            var lines = joinedText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Every line should now be a key-value pair
            foreach (var line in lines)
            {
                var index = line.IndexOf(SEPARATOR);

                if (index < 0)
                {
                    throw new InvalidOperationException(
                        string.Format(System.Globalization.CultureInfo.CurrentCulture, UIResources.Reader_Error_InvalidManifest, line));
                }

                var key = line.Substring(0, index);
                var value = line.Substring(index + SEPARATOR.Length);
                kvps[key] = value;
            }
        }

        public string FindValue(string key)
        {
            kvps.TryGetValue(key, out string value);
            return value;
        }

        public string GetValue(string key)
        {
            if (!kvps.TryGetValue(key, out string value))
            {
                throw new InvalidOperationException(
                    string.Format(System.Globalization.CultureInfo.CurrentCulture, UIResources.Reader_Error_MissingManifestSetting, key));
            }
            return value;
        }
    }
}
