/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2024 SonarSource SA
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
using SonarQube.Plugins.Roslyn.CommandLine;
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
        /// <param name="configDirectory">Optional. Specifies an additional directory in which to look for a NuGet.config file</param>
        public static ISettings GetSettingsFromConfigFiles(string configDirectory)
        {
            IFileSystem fileSystem = null;
            if (configDirectory != null)
            {
                fileSystem = new PhysicalFileSystem(configDirectory);
            }
            NuGetMachineWideSettings machineSettings = new NuGetMachineWideSettings();
            ISettings settings = Settings.LoadDefaultSettings(fileSystem, null, machineSettings);
            return settings;
        }

        /// <summary>
        /// Creates and returns an aggregate repository using the specified settings
        /// </summary>
        public static IPackageRepository CreateRepository(ISettings settings, Common.ILogger logger)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
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
            AggregateRepository aggRepo = packageSourceProvider.CreateAggregateRepository(PackageRepositoryFactory.Default,
                true /* ignore failing repos. Errors will be logged as warnings. */ );
            aggRepo.Logger = new NuGetLoggerAdapter(logger);

            return aggRepo;
        }

        /// <summary>
        /// Creates a NuGet repo depending on the given command line arguments for the customnugetrepo path.
        /// </summary>
        public static IPackageRepository CreateRepositoryForArguments(Common.ILogger logger, ProcessedArgs processedArgs, string defaultNuGetPath)
        {
            IPackageRepository repo;

            if (string.IsNullOrWhiteSpace(processedArgs.CustomNuGetRepository))
            {
                ISettings nuGetSettings = GetSettingsFromConfigFiles(defaultNuGetPath);
                repo = CreateRepository(nuGetSettings, logger);
            }
            else
            {
                logger.LogDebug(UIResources.NG_UsingCustomNuGetFolder, processedArgs.CustomNuGetRepository);
                repo = PackageRepositoryFactory.Default.CreateRepository(processedArgs.CustomNuGetRepository);
            }

            return repo;
        }
    }
}