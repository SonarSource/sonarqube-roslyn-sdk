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

using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Updates an existing archive (zip, jar) by inserting additional files
    /// </summary>
    public class ArchiveUpdater
    {
        private readonly string workingDirectory;
        private readonly IDictionary<string, string> fileMap;
        private readonly ILogger logger;

        private string inputArchiveFilePath;
        private string outputArchiveFilePath;

        #region Public methods

        public ArchiveUpdater(string workingDirectory, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentNullException("workingDirectory");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.logger = logger;
            this.workingDirectory = workingDirectory;

            this.fileMap = new Dictionary<string, string>();
        }

        public ArchiveUpdater SetInputArchive(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }
            this.inputArchiveFilePath = filePath;
            return this;
        }

        public ArchiveUpdater SetOutputArchive(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }
            this.outputArchiveFilePath = filePath;
            return this;
        }

        public ArchiveUpdater AddFile(string sourceFilePath, string relativeTargetFilePath)
        {
            this.fileMap[relativeTargetFilePath] = sourceFilePath;
            return this;
        }

        public void UpdateArchive()
        {
            this.logger.LogDebug(UIResources.ZIP_UpdatingArchive, this.inputArchiveFilePath);

            string unpackedDir = Utilities.CreateSubDirectory(this.workingDirectory, "unpacked");
            this.logger.LogDebug(UIResources.ZIP_WorkingDirectory, workingDirectory);

            ZipFile.ExtractToDirectory(this.inputArchiveFilePath, unpackedDir);

            // Add in the new files
            foreach (KeyValuePair<string, string> kvp in this.fileMap)
            {
                this.logger.LogDebug(UIResources.ZIP_InsertingFile, kvp.Key, kvp.Value);

                string targetFilePath = Path.Combine(unpackedDir, kvp.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
                File.Copy(kvp.Value, targetFilePath);
            }

            // Re-zip
            if (File.Exists(this.outputArchiveFilePath))
            {
                this.logger.LogDebug(UIResources.ZIP_DeletingExistingArchive);
                File.Delete(this.outputArchiveFilePath);
            }

            ZipUsingShell(unpackedDir, this.outputArchiveFilePath);

            this.logger.LogDebug(UIResources.ZIP_NewArchiveCreated, this.outputArchiveFilePath);
        }

        /// <summary>
        /// Zips the folder using the shell
        /// </summary>
        /// <remarks>Re-zipping a jar file using the .Net ZipFile class creates an invalid jar
        /// so we zip using the shell instead.
        /// The code is doing effectively the same as the PowerShell script used by the 
        /// packaging project in the SonarQube Scanner for MSBuild:
        /// See https://github.com/SonarSource-VisualStudio/sonar-msbuild-runner/blob/master/PackagingProjects/CSharpPluginPayload/RepackageCSharpPlugin.ps1
        /// </remarks>
        private static void ZipUsingShell(string sourceDir, string targetZipFilePath)
        {
            // The Folder.CopyHere method for Shell Objects allows configuration based on a combination of flags.
            // Docs here: https://msdn.microsoft.com/en-us/library/windows/desktop/bb787866(v=vs.85).aspx
            // The value below (1556) consists of
            //    (4)    - no progress dialog
            //    (16)   - respond with "yes to all" to any dialog box
            //    (512)  - Do not confirm the creation of a new directory
            //    (1024) - Do not display an UI in case of error
            const int copyFlags = 1556;

            const int copyPauseInMilliseconds = 500;

            // The file must have a ".zip" extension for the shell code below to work
            string zipFilePath = targetZipFilePath + ".zip";
            CreateEmptyZipFile(zipFilePath);

            Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
            Object app = Activator.CreateInstance(shellAppType);

            Shell32.Folder folder = shellAppType.InvokeMember("NameSpace", System.Reflection.BindingFlags.InvokeMethod, null, app, new object[] { zipFilePath }) as Shell32.Folder;

            foreach(string dir in Directory.GetDirectories(sourceDir))
            {
                folder.CopyHere(dir, copyFlags);
                System.Threading.Thread.Sleep(copyPauseInMilliseconds);
            }
            
            foreach (string file in Directory.GetFiles(sourceDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                folder.CopyHere(file, copyFlags);
                System.Threading.Thread.Sleep(copyPauseInMilliseconds);
            }

            // Rename the file to the expected name
            File.Move(zipFilePath, targetZipFilePath);
        }

        private static void CreateEmptyZipFile(string filePath)
        {
            byte[] emptyZipHeader = new byte[] { 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            using (FileStream fs = File.Create(filePath))
            {
                fs.Write(emptyZipHeader, 0, emptyZipHeader.Length);
                fs.Flush();
            }
        }

        #endregion
    }
}
