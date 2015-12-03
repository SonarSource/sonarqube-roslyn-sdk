//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Roslyn.SonarQube.AnalyzerPlugins.CommandLine;
using Roslyn.SonarQube.Common;

namespace Roslyn.SonarQube.AnalyzerPlugins
{
    /// <summary>
    /// Generates SonarQube plugins for Roslyn analyzers
    /// </summary>
    class Program
    {
        private const int ERROR_CODE = 1;
        private const int SUCCESS_CODE = 0;

        static int Main(string[] args)
        {
            ConsoleLogger logger = new ConsoleLogger();
            Common.Utilities.LogAssemblyVersion(typeof(Program).Assembly, UIResources.AssemblyDescription, logger);
            
            ProcessedArgs processedArgs = ArgumentProcessor.TryProcessArguments(args, logger);

            bool success = false;
            if (processedArgs != null)
            {
                AnalyzerPluginGenerator generator = new AnalyzerPluginGenerator(logger);
                success = generator.Generate(processedArgs.AnalyzerRef, processedArgs.SqaleFilePath);
            }

            return success ? SUCCESS_CODE : ERROR_CODE;
        }
    }
}
