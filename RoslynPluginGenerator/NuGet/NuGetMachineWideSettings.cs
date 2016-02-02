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
