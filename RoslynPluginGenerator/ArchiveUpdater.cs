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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using SonarQube.Plugins.Common;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Updates an existing archive (zip, jar) by inserting additional files
    /// </summary>
    public class ArchiveUpdater
    {
        private readonly IDictionary<string, string> fileMap;
        private readonly ILogger logger;

        private string inputArchiveFilePath;
        private string outputArchiveFilePath;

        #region Public methods

        public ArchiveUpdater(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            fileMap = new Dictionary<string, string>();
        }

        public ArchiveUpdater SetInputArchive(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            inputArchiveFilePath = filePath;
            return this;
        }

        public ArchiveUpdater SetOutputArchive(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            outputArchiveFilePath = filePath;
            return this;
        }

        public ArchiveUpdater AddFile(string sourceFilePath, string relativeTargetFilePath)
        {
            fileMap[relativeTargetFilePath] = sourceFilePath;
            return this;
        }

        public void UpdateArchive()
        {
            if (File.Exists(outputArchiveFilePath))
            {
                logger.LogDebug(UIResources.ZIP_DeletingExistingJar);
                File.Delete(outputArchiveFilePath);
            }
            File.Copy(inputArchiveFilePath, outputArchiveFilePath);

            logger.LogDebug(UIResources.ZIP_UpdatingJar, inputArchiveFilePath);
            DoUpdate();
            logger.LogDebug(UIResources.ZIP_JarUpdated, outputArchiveFilePath);
        }

        #endregion Public methods

        private void DoUpdate()
        {
            using (ZipArchive newArchive = new ZipArchive(new FileStream(outputArchiveFilePath, FileMode.Open), ZipArchiveMode.Update))
            {
                // Add/update the new files
                foreach (KeyValuePair<string, string> kvp in fileMap)
                {
                    var data = File.ReadAllBytes(kvp.Value);

                    string canonicalKey = kvp.Key.Replace("\\", "/");
                    var entry = GetOrCreateEntry(newArchive, canonicalKey);

                    logger.LogDebug(UIResources.ZIP_InsertingFile, kvp.Key, kvp.Value);
                    using (var entryStream = entry.Open())
                    {
                        entryStream.Write(data, 0, data.Length);
                        entryStream.Flush();
                    }
                }
            }
        }

        private ZipArchiveEntry GetOrCreateEntry(ZipArchive archive, string fullEntryName)
        {
            var existingEntry = archive.GetEntry(fullEntryName);
            if (existingEntry != null)
            {
                logger.LogDebug(UIResources.ZIP_DeleteExistingEntry, fullEntryName);
                existingEntry.Delete();
            }

            logger.LogDebug(UIResources.ZIP_CreatingNewEntry, fullEntryName);
            return archive.CreateEntry(fullEntryName);

        }
    }
}