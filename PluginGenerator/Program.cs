using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginGenerator
{
    class Program
    {
        internal const int ERROR_CODE = -1;
        internal const int SUCCESS_CODE = 0;

        static int Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();
            if (args.Length != 1)
            {
                logger.LogError(UIResources.Cmd_Error_IncorrectArguments);
                return ERROR_CODE;
            }

            PluginDefinition defn = PluginDefinition.Load(args[0]);
            string outputDir = Directory.GetCurrentDirectory();

            PluginGenerator generator = new PluginGenerator();
            generator.GeneratePlugin(defn, outputDir, logger);

            return SUCCESS_CODE;
        }
    }
}
