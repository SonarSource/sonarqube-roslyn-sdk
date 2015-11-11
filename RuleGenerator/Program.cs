using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // Retrieve optional user-specified additional assembly search directories
            string additionalSearchFolderRawString = ProgramSettings.Default.AdditionalDependencySearchFolders;
            DiagnosticAssemblyScanner scanner = null;

            if (!String.IsNullOrWhiteSpace(additionalSearchFolderRawString))
            {
                IEnumerable<string> additionalSearchFolders = new List<string>(additionalSearchFolderRawString.Split(','));

                additionalSearchFolders = from folder in additionalSearchFolders
                                          where Directory.Exists(folder) == true
                                          select folder;
                if (additionalSearchFolders.Any())
                {
                    logger.LogInfo(Resources.INFO_AdditionalDependencySearchFoldersFound, additionalSearchFolders.Count());
                    scanner = new DiagnosticAssemblyScanner(additionalSearchFolders, logger);
                } else
                {
                    logger.LogWarning(Resources.WARN_NoValidAdditionalDependencySearchFolders);
                }
            }

            // If no additional valid search directories were specified, use default constructor
            if (scanner == null)
            {
                scanner = new DiagnosticAssemblyScanner(logger);
            }

            IEnumerable<DiagnosticAnalyzer> diagnostics = scanner.InstantiateDiagnosticsFromAssembly(assemblyPath, language);

            Debug.Assert(diagnostics != null);
            if (diagnostics.Any())
            {
                IRuleGenerator ruleGenerator = new RuleGenerator(logger);
                Rules rules = ruleGenerator.GenerateRules(diagnostics);

                string outputFile = Path.ChangeExtension(assemblyPath, ".xml");
                rules.Save(outputFile, logger);
                logger.LogInfo(Resources.SuccessOutputFile, rules.Count, outputFile);
                logger.LogInfo(Resources.RuleGenerationSuccess);
            }
        }

        private static void PrintUsage(ILogger logger)
        {
            logger.LogInfo(Resources.CommandLineUsage, AppDomain.CurrentDomain.FriendlyName);
        }
    }
}