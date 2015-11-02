using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Roslyn.SonarQube
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();

            if (args == null || args.Length != 1)
            {
                PrintUsage(logger);
                return;
            }

            string assemblyPath = args[0];
            if (!File.Exists(assemblyPath))
            {
                logger.LogError(Resources.ERR_ArgFileDoesNotExist, assemblyPath);
                return;
            }

            DiagnosticAssemblyScanner scanner = new DiagnosticAssemblyScanner(logger);
            IEnumerable<DiagnosticAnalyzer> diagnostics = scanner.ExtractDiagnosticsFromAssembly(assemblyPath);

            if (diagnostics.Any())
            {
                IRuleGenerator ruleGenerator = new RuleGenerator(logger);
                Rules rules = ruleGenerator.GenerateRules(diagnostics);

                string outputFile = Path.ChangeExtension(assemblyPath, ".xml");
                rules.Save(outputFile, logger);
            }
            else
            {
                logger.LogError(Resources.NoAnalysers);
            }
        }

        private static void PrintUsage(ILogger logger)
        {
            logger.LogInfo(Resources.CommandLineUsage, AppDomain.CurrentDomain.FriendlyName);
        }
    }
}