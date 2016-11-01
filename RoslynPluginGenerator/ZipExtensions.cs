//-----------------------------------------------------------------------
// <copyright file="ZipExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

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
                throw new ArgumentNullException("sourceDirectoryName");
            }
            if (string.IsNullOrWhiteSpace(destinationArchiveFileName))
            {
                throw new ArgumentNullException("destinationArchiveFileName");
            }
            if (fileInclusionPredicate == null)
            {
                throw new ArgumentNullException("fileInclusionPredicate");
            }

            string[] files = Directory.GetFiles(sourceDirectoryName, "*.*", SearchOption.AllDirectories);

            int pathPrefixLength = sourceDirectoryName.Length + 1;
            using (ZipArchive archive = ZipFile.Open(destinationArchiveFileName, ZipArchiveMode.Create))
            {
                foreach (string file in files.Where(f => fileInclusionPredicate(f)))
                {
                    archive.CreateEntryFromFile(file, file.Substring(pathPrefixLength), CompressionLevel.Optimal);
                }
            }
        }
    }
}
