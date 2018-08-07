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

using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet;
using SonarQube.Plugins.Common;
using SonarQube.Plugins.Roslyn.CommandLine;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Generates SonarQube plugins for Roslyn analyzers
    /// </summary>
    public static class Program
    {
        private const int ERROR_CODE = 1;
        private const int SUCCESS_CODE = 0;

        private static int Main(string[] args)
        {
            ConsoleLogger logger = new ConsoleLogger();
            Utilities.LogAssemblyVersion(typeof(Program).Assembly, UIResources.Program_AssemblyDescription, logger);
            Utilities.LogAssemblyVersion(typeof(DiagnosticAnalyzer).Assembly, UIResources.Program_SupportedRoslynVersion, logger);
            logger.LogInfo(UIResources.Program_SupportedSonarQubeVersions);

            ProcessedArgs processedArgs = ArgumentProcessor.TryProcessArguments(args, logger);

            bool success = false;
            if (processedArgs != null)
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                ISettings nuGetSettings = NuGetRepositoryFactory.GetSettingsFromConfigFiles(exeDir);
                IPackageRepository repo = NuGetRepositoryFactory.CreateRepository(nuGetSettings, logger);
                string localNuGetCache = Utilities.CreateTempDirectory(".nuget");
                NuGetPackageHandler packageHandler = new NuGetPackageHandler(repo, localNuGetCache, logger);

                AnalyzerPluginGenerator generator = new AnalyzerPluginGenerator(packageHandler, logger);
                success = generator.Generate(processedArgs);
            }

            return success ? SUCCESS_CODE : ERROR_CODE;
        }
    }
}