//-----------------------------------------------------------------------
// <copyright file="NuGetMachineWideSettings.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
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
    internal class NuGetMachineWideSettings : IMachineWideSettings
    {
        private const string ConfigDir = "SonarQube";

        private readonly Settings[] machineSettings;

        public NuGetMachineWideSettings()
        {
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            this.machineSettings = Settings.LoadMachineWideSettings(new PhysicalFileSystem(baseDir), ConfigDir).ToArray();
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
