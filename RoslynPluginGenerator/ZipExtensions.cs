/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2022 SonarSource SA
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SonarQube.Plugins.Roslyn
{
    public static class ZipExtensions
    {
        /// <summary>
        /// Creates a zip file from the specified directory, filtering out files that don't
        /// match the supplied check
        /// </summary>
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, Func<string, bool> fileInclusionPredicate)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectoryName))
            {
                throw new ArgumentNullException(nameof(sourceDirectoryName));
            }
            if (string.IsNullOrWhiteSpace(destinationArchiveFileName))
            {
                throw new ArgumentNullException(nameof(destinationArchiveFileName));
            }
            if (fileInclusionPredicate == null)
            {
                throw new ArgumentNullException(nameof(fileInclusionPredicate));
            }

            string[] files = Directory.GetFiles(sourceDirectoryName, "*.*", SearchOption.AllDirectories);

            int pathPrefixLength = sourceDirectoryName.Length + 1;

            using (ZipArchive archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create, new PathEncoder()))
            {
                foreach (string file in files.Where(f => fileInclusionPredicate(f)))
                {
                    archive.CreateEntryFromFile(file, file.Substring(pathPrefixLength), CompressionLevel.Optimal);
                }
            }
        }
    }

    /// <summary>
    /// Replaces the back slash to foward slash to attend the international convention of directories separation.
    /// This will allow the zip file to be unzipped the correct way both linux and windows OS's.
    /// </summary>
    /// <seealso cref="System.Text.UTF8Encoding" />
    class PathEncoder : UTF8Encoding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PathEncoder"/> class.
        /// </summary>
        public PathEncoder() : base(true)
        {
        }

        /// <summary>
        /// When overridden in a derived class, encodes all the characters in the specified string into a sequence of bytes.
        /// </summary>
        /// <param name="s">The string containing the characters to encode.</param>
        /// <returns>
        /// A byte array containing the results of encoding the specified set of characters.
        /// </returns>
        public override byte[] GetBytes(string s)
        {
            s = s.Replace("\\", "/");
            return base.GetBytes(s);
        }
    }
}