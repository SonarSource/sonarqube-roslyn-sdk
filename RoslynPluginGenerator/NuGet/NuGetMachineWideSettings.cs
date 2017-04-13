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

using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Simple implementation of <see cref="IMachineWideSettings"/>.
    /// Returns any settings in the standard machine-wide NuGet settings folders, together
    /// with any that exist in the "SonarQube" sub-directory
    /// </summary>
    public class NuGetMachineWideSettings : IMachineWideSettings
    {
        public const string SonarQubeConfigSubDirName = "SonarQube";

        private readonly Settings[] machineSettings;

        public NuGetMachineWideSettings()
            : this(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData))
        {
        }

        /// <summary>
        /// Constructor used for testing
        /// </summary>
        public NuGetMachineWideSettings(string baseDir)
        {
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                throw new ArgumentNullException("baseDir");
            }
            this.machineSettings = Settings.LoadMachineWideSettings(new PhysicalFileSystem(baseDir), SonarQubeConfigSubDirName).ToArray();
        }

        #region IMachineWideSettings methods

        IEnumerable<Settings> IMachineWideSettings.Settings
        {
            get
            {
                return this.machineSettings;
            }
        }

        #endregion
    }
}
