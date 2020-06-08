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
using System.IO;
using System.Reflection;

namespace SonarQube.Plugins.Common
{
    public static class Utilities
    {
        private const string DllExtension = ".dll";

        public static void LogAssemblyVersion(Assembly assembly, string description, ILogger logger)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.LogInfo("{0} {1}", description, assembly.GetName().Version);
        }

        public static string CreateTempDirectory(string dirName)
        {
            string newPath = Path.GetTempPath();
            newPath = Path.Combine(newPath, ".sonarqube.sdk", dirName);
            Directory.CreateDirectory(newPath);
            return newPath;
        }

        public static string CreateSubDirectory(string parent, string child)
        {
            if (string.IsNullOrWhiteSpace(parent))
            {
                throw new ArgumentNullException(nameof(parent));
            }
            if (string.IsNullOrWhiteSpace(child))
            {
                throw new ArgumentNullException(nameof(child));
            }

            string newDir = Path.Combine(parent, child);
            Directory.CreateDirectory(newDir);
            return newDir;
        }

        /// <summary>
        /// Returns whether the supplied string is an assembly library (i.e. dll)
        /// </summary>
        public static bool IsAssemblyLibraryFileName(string filePath)
        {
            // Not expecting .winmd or .exe files to contain Roslyn analyzers
            // so we'll ignore them
            return filePath.EndsWith(DllExtension, StringComparison.OrdinalIgnoreCase);
        }

        public static void CleanCacheForPackage(string localCacheRoot, string assemblyName, string version)
        {
            string assemblyDir = Path.Combine(localCacheRoot, string.Format("{0}.{1}", assemblyName, version));
            if (Directory.Exists(assemblyDir))
            {
                Directory.Delete(assemblyDir, true);
            }
        }
    }
}