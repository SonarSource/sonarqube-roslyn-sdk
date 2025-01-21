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

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SonarQube.Common
{
    /// <summary>
    /// Container for all native dll definitions and calls
    /// </summary>
    internal static class NativeMethods
    {
        public const int MAXPATH = 260; // maximum length of path in Windows

        //BOOL PathFindOnPath(_Inout_   LPTSTR pszFile, _In_opt_  LPCTSTR *ppszOtherDirs)
        [DllImport("Shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool PathFindOnPath([In, Out] StringBuilder fileName, [In]string[] otherDirs);

        #region Public methods

        /// <summary>
        /// Searches the directories defined in the PATH environment variable for the specified file
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <returns>The full path to the file, or null if the file could not be located</returns>
        public static string FindOnPath(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            Debug.Assert(fileName.Equals(Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase), "Parameter should be a file name i.e. should not include any path elements");

            StringBuilder sb = new StringBuilder(fileName, MAXPATH);

            bool found = PathFindOnPath(sb, null);
            if (found)
            {
                return sb.ToString();
            }
            return null;
        }

        #endregion Public methods
    }
}