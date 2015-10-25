using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.SonarQube.PluginGenerator
{
    static class Program
    {
        internal const int ERROR_CODE = -1;
        internal const int SUCCESS_CODE = 0;

        static int Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();

            Utilities.LogAssemblyVersion(typeof(Program).Assembly, UIResources.AssemblyDescription, logger);

            if (args.Length != 2)
            {
                logger.LogError(UIResources.Cmd_Error_IncorrectArguments);
                return ERROR_CODE;
            }

            string pluginDefnFilePath = args[0];
            string rulesFilePath = args[1];

            PluginDefinition defn = PluginDefinition.Load(pluginDefnFilePath);
            string fullNewJarFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                Path.GetFileNameWithoutExtension(pluginDefnFilePath) + ".jar");

            RulesPluginGenerator generator = new RulesPluginGenerator(new JdkWrapper(), logger);
            generator.GeneratePlugin(defn, rulesFilePath, fullNewJarFilePath);

            return SUCCESS_CODE;
        }
    }
}
