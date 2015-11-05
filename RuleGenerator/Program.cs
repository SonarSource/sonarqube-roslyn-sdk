using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Roslyn.SonarQube
{
    internal static class Program
    {
        // TODO: multiple language support
        private static readonly string language = LanguageNames.CSharp;

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
            IEnumerable<DiagnosticAnalyzer> diagnostics = scanner.ExtractDiagnosticsFromAssembly(assemblyPath, language);

            if (diagnostics.Any())
            {
                IRuleGenerator ruleGenerator = new RuleGenerator(logger);
                Rules rules = ruleGenerator.GenerateRules(diagnostics);

                string outputFile = Path.ChangeExtension(assemblyPath, ".xml");
                rules.Save(outputFile, logger);
                logger.LogInfo(Resources.SuccessOutputFile, rules.Count, outputFile);
                logger.LogInfo(Resources.SuccessStatus);
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