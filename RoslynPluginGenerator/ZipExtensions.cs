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

using System.IO.Compression;

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
            using (ZipArchive archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
            {
                foreach (string file in files.Where(f => fileInclusionPredicate(f)))
                {
                    archive.CreateEntryFromFile(file, file.Substring(pathPrefixLength).Replace('\\', '/'), CompressionLevel.Optimal);
                }
            }
        }
    }
}