//-----------------------------------------------------------------------
// <copyright file="NuGetRepositoryFactory.cs" company="SonarSource SA and Microsoft Corporation">
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
    /// Creates the remote repository from which the packages will be fetched
    /// </summary>
    public static class NuGetRepositoryFactory
    {
        /// <summary>
        /// Returns the settings loaded from the user- and machine-wide config files
        /// </summary>
        public static ISettings GetSettingsFromConfigFiles()
        {
            NuGetMachineWideSettings machineSettings = new NuGetMachineWideSettings();
            ISettings settings = Settings.LoadDefaultSettings(null, null, machineSettings);
            return settings;
        }

        /// <summary>
        /// Creates and returns an aggregate repository using the specified settings
        /// </summary>
        public static IPackageRepository CreateRepository(ISettings settings, Common.ILogger logger)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            // Load the user and machine-wide settings
            logger.LogDebug(UIResources.NG_FetchingConfigFiles);

            // Get a package source provider that can use the settings
            PackageSourceProvider packageSourceProvider = new PackageSourceProvider(settings);

            logger.LogDebug(UIResources.NG_ListingEnablePackageSources);
            IEnumerable<PackageSource> enabledSources = packageSourceProvider.GetEnabledPackageSources();
            if (!enabledSources.Any())
            {
                logger.LogWarning(UIResources.NG_NoEnabledPackageSources);
            }
            else
            {
                foreach (PackageSource enabledSource in enabledSources)
                {
                    logger.LogDebug(UIResources.NG_ListEnabledPackageSource, enabledSource.Source, enabledSource.IsMachineWide);
                }
            }
            // Create an aggregate repository that uses all of the configured sources
            AggregateRepository aggRepo = packageSourceProvider.CreateAggregateRepository(PackageRepositoryFactory.Default, true /* ignore failing repos */ );

            return aggRepo;
        }
    }
}
