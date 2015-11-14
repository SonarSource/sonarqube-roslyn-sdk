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
                success = generator.Generate(processedArgs.AnalyzerRef);
            }

            return success ? SUCCESS_CODE : ERROR_CODE;
        }
    }
}
