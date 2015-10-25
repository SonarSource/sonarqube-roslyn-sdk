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
            
            string packageId;
            NuGet.SemanticVersion packageVersion;
            switch (args.Length)
            {
                case 2:
                    packageId = args[0];

                    if (!NuGet.SemanticVersion.TryParse(args[1], out packageVersion))
                    {
                        logger.LogError(UIResources.CmdLine_ERROR_InvalidVersion, args[1]);
                        return ERROR_CODE;
                    }
                    break;
                case 1:
                    packageId = args[0];
                    packageVersion = null;
                    break;
                default:
                    logger.LogError(UIResources.CmdLine_ERROR_InvalidArgumentCount);
                    return ERROR_CODE;
            }

            AnalyzerPluginGenerator generator = new AnalyzerPluginGenerator(logger);

            bool success = generator.Generate(packageId, packageVersion);

            return success ? SUCCESS_CODE : ERROR_CODE;
        }
    }
}
