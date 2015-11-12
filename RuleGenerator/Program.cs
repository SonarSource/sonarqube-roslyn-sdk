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
    public class Program
    {
        // TODO: multiple language support
        private static readonly string language = LanguageNames.CSharp;

        public Program(ILogger logger, string assemblyPath)
        {
            // Retrieve optional user-specified additional assembly search directories
            string additionalSearchFolderRawString = ProgramSettings.Default.AdditionalDependencySearchFolders;
            DiagnosticAssemblyScanner scanner = null;
            
            IEnumerable<string> validSearchPaths = ParseAndValidateSearchPaths(LoadSearchPathsFromSettings());
            
            if (validSearchPaths.Any())
            {
                logger.LogInfo(Resources.INFO_AdditionalDependencySearchFoldersFound, validSearchPaths.Count());
                scanner = new DiagnosticAssemblyScanner(validSearchPaths, logger);
            }
            else
            {
                logger.LogWarning(Resources.WARN_NoValidAdditionalDependencySearchFolders);
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

            new Program(logger, assemblyPath);
        }

        private IEnumerable<string> LoadSearchPathsFromSettings()
        {
            string additionalSearchFolderRawString = ProgramSettings.Default.AdditionalDependencySearchFolders;
            IEnumerable<string> additionalSearchFolders = null;

            if (!String.IsNullOrWhiteSpace(additionalSearchFolderRawString))
            {
                additionalSearchFolders = new List<string>(additionalSearchFolderRawString.Split(','));
            }

            return additionalSearchFolders ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Attempts to parse and validate a list of directory paths.
        /// </summary>
        /// <returns>
        /// A subset of the input where every member is a valid path. 
        /// </returns>
        public static IEnumerable<string> ParseAndValidateSearchPaths(IEnumerable<string> inputPaths)
        {
            return from inputPath in inputPaths
                   where Directory.Exists(inputPath) == true
                   select inputPath;
        }

        private static void PrintUsage(ILogger logger)
        {
            logger.LogInfo(Resources.CommandLineUsage, AppDomain.CurrentDomain.FriendlyName);
        }
    }
}