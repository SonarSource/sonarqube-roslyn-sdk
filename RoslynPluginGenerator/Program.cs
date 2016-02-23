//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using NuGet;
using SonarQube.Plugins.Common;
using SonarQube.Plugins.Roslyn.CommandLine;
using System.IO;
using System.Reflection;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Generates SonarQube plugins for Roslyn analyzers
    /// </summary>
    public static class Program
    {
        private const int ERROR_CODE = 1;
        private const int SUCCESS_CODE = 0;

        static int Main(string[] args)
        {
            ConsoleLogger logger = new ConsoleLogger();
            Utilities.LogAssemblyVersion(typeof(Program).Assembly, UIResources.AssemblyDescription, logger);
            
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
