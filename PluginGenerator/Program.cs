//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Plugins.Common;
using System.IO;

namespace SonarQube.Plugins.PluginGenerator
{
    static class Program
    {
        internal const int ERROR_CODE = -1;
        internal const int SUCCESS_CODE = 0;

        static int Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();

            Utilities.LogAssemblyVersion(typeof(Program).Assembly, UIResources.AssemblyDescription, logger);

            if (args.Length != 2 && args.Length != 3)
            {
                logger.LogError(UIResources.Cmd_Error_IncorrectArguments);
                return ERROR_CODE;
            }

            string pluginDefnFilePath = args[0];
            string rulesFilePath = args[1];
                        
            PluginDefinition defn = PluginDefinition.Load(pluginDefnFilePath);
            string fullNewJarFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                Path.GetFileNameWithoutExtension(pluginDefnFilePath) + ".jar");

            string sqaleFilePath = null;
            if (args.Length == 3)
            {
                sqaleFilePath = args[2];
            }

            PluginBuilder builder = new PluginBuilder(logger);
            RulesPluginBuilder.ConfigureBuilder(builder, defn, rulesFilePath, sqaleFilePath);
            builder.Build();

            return SUCCESS_CODE;
        }
    }
}
