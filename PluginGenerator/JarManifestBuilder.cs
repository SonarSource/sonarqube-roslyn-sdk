//-----------------------------------------------------------------------
// <copyright file="JarManifestBuilder.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SonarQube.Plugins
{
    /// <summary>
    /// Builds a valid v1.0 Jar Manifest.
    /// See http://docs.oracle.com/javase/6/docs/technotes/guides/jar/jar.html#JAR%20Manifest
    /// </summary>
    public class JarManifestBuilder
    {
        /// <summary>
        /// Magic string that must appear first in the file
        /// </summary>
        private const string ManifestVersionEntry = "Manifest-Version: 1.0";

        /// <summary>
        /// Required manifest file name
        /// </summary>
        private const string ManifestFileName = "MANIFEST.MF";

        private const string ValidNamePattern = "^[0-9a-zA-Z_-]+$";
        private const int MaxNameLength = 70;
        private const int MaxLineLength = 72;

        private readonly IDictionary<string, string> properties = new Dictionary<string, string>();

        #region Public methods

        /// <summary>
        /// Sets the value of a property in the manifest file.
        /// Any existing value will be overwritten.
        /// </summary>
        public void SetProperty(string name, string value)
        {
            CheckAttributeNameIsValid(name);

            this.properties[name] = value;
        }

        public bool TryGetValue(string name, out string value)
        {
            return this.properties.TryGetValue(name, out value);
        }

        /// <summary>
        /// Builds and returns the manifest
        /// </summary>
        public string BuildManifest()
        {
            StringBuilder sb = new StringBuilder();

            // Write the main attributes
            sb.AppendLine(ManifestVersionEntry);
            
            // Write the remaining attributes
            foreach(KeyValuePair<string, string> kvp in this.properties)
            {
                WriteEntry(kvp.Key, kvp.Value, sb);
            }

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Writes the manifest file to the specified directory
        /// </summary>
        public string WriteManifest(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentNullException(nameof(directory));
            }

            // Ensure the directory exists
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, ManifestFileName);

            File.WriteAllText(filePath, this.BuildManifest());

            return filePath;
        }

        #endregion

        #region Private methods

        private static void CheckAttributeNameIsValid(string name)
        {
            // [0-9a-zA-Z_-]
            // 70 chars max
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length > MaxNameLength)
            {
                string message = string.Format(System.Globalization.CultureInfo.CurrentCulture, UIResources.JMan_Error_NameTooLong, name);
                throw new ArgumentException(message, nameof(name));
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(name, ValidNamePattern))
            {
                string message = string.Format(System.Globalization.CultureInfo.CurrentCulture, UIResources.JMan_Error_InvalidCharsInName, name);
                throw new ArgumentException(message, nameof(name));
            }
        }

        /// <summary>
        /// Writes an entry, splitting it across multiple lines if required
        /// </summary>
        /// <remarks>NOTE: we assuming there are no multi-byte characters in the value. If there are,
        /// the string won't be split correctly</remarks>
        private static void WriteEntry(string key, string value, StringBuilder sb)
        {
            string text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}: {1}", key, value);

            Debug.Assert(!value.Any(c => char.IsSurrogate(c)), "Not expecting multi-byte characters");

            if (text.Length <= MaxLineLength)
            {
                sb.AppendLine(text);
                return;
            }

            // Data is too long a single line
            // Write the first line without a continuation character
            sb.AppendLine(text.Substring(0, MaxLineLength));
            text = text.Substring(MaxLineLength);

            // For every remaining line, we need to reserve characters
            // for the continuation prefix
            const string continuationPrefix = " ";
            int maxActualCharsPerLine = MaxLineLength - continuationPrefix.Length;

            while (text.Length > 0)
            {
                // Make sure we don't try to take too many characters
                int charsToTake = Math.Min(text.Length, maxActualCharsPerLine);

                sb.AppendLine(continuationPrefix + text.Substring(0, charsToTake));
                text = text.Substring(charsToTake);
            }
        }

        #endregion
    }
}
