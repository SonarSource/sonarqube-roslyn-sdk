using Roslyn.SonarQube.Common;
using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Roslyn.SonarQube.AnalyzerPlugins.CommandLine
{
    public class ArgumentProcessor
    {
        #region Argument definitions

        /// <summary>
        /// Ids for supported arguments
        /// </summary>
        private static class KeywordIds
        {
            public const string AnalyzerRef = "analyzer.ref";
            public const string SqaleXmlFile = "sqale.xml";
        }

        private static IList<ArgumentDescriptor> Descriptors;

        static ArgumentProcessor()
        {
            // Initialise the set of valid descriptors.
            // To add a new argument, just add it to the list.
            Descriptors = new List<ArgumentDescriptor>();

            Descriptors.Add(new ArgumentDescriptor(
                id: KeywordIds.AnalyzerRef, prefixes: new string[] { "/analyzer:", "/a:" }, required: true, allowMultiple: false, description: CmdLineResources.ArgDescription_AnalzyerRef));
            Descriptors.Add(new ArgumentDescriptor(
                id: KeywordIds.SqaleXmlFile, prefixes: new string[] { "/sqale:", "/s:" }, required: false, allowMultiple: false, description: CmdLineResources.ArgDescription_SqaleXmlFile));

            Debug.Assert(Descriptors.All(d => d.Prefixes != null && d.Prefixes.Any()), "All descriptors must provide at least one prefix");
            Debug.Assert(Descriptors.Select(d => d.Id).Distinct().Count() == Descriptors.Count, "All descriptors must have a unique id");
        }

        #endregion Argument definitions

        public static ProcessedArgs TryProcessArguments(string[] commandLineArgs, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            ArgumentProcessor processor = new ArgumentProcessor(logger);
            return processor.Process(commandLineArgs);
        }

        private readonly ILogger logger;

        private ArgumentProcessor(ILogger logger)
        {
            this.logger = logger;
        }

        public ProcessedArgs Process(string[] commandLineArgs)
        {
            ProcessedArgs processed = null;
            IEnumerable<ArgumentInstance> arguments;

            // This call will fail if there are duplicate, missing, or unrecognized arguments
            CommandLineParser parser = new CommandLineParser(Descriptors, false /* don't allow unrecognized */);
            bool parsedOk = parser.ParseArguments(commandLineArgs, this.logger, out arguments);

            NuGetReference analyzerRef;
            parsedOk &= TryParseAnalyzerRef(arguments, out analyzerRef);

            string sqaleFilePath;
            parsedOk &= TryParseSqaleFile(arguments, out sqaleFilePath);

            if (parsedOk)
            {
                Debug.Assert(analyzerRef != null, "Expecting to have a valid analyzer reference");
                processed = new ProcessedArgs(analyzerRef, sqaleFilePath);
            }

            return processed;
        }

        private bool TryParseAnalyzerRef(IEnumerable<ArgumentInstance> arguments, out NuGetReference analyzerRef)
        {
            analyzerRef = null;
            ArgumentInstance analyzerArg = arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.AnalyzerRef, a.Descriptor.Id));

            if (analyzerArg != null)
            {
                analyzerRef = TryParseNuGetReference(analyzerArg.Value);
            }
            return analyzerRef != null;
        }

        private NuGetReference TryParseNuGetReference(string argumentValue)
        {
            // The argument value will be in one of the following forms:
            // [package id]
            // [package id]:[version]

            string packageId;
            NuGet.SemanticVersion packageVersion = null;

            int lastIndex = argumentValue.LastIndexOf(':');
            if (lastIndex > -1)
            {
                packageId = argumentValue.Substring(0, lastIndex);
                string rawVersion = argumentValue.Substring(lastIndex + 1);

                if (string.IsNullOrWhiteSpace(packageId))
                {
                    this.logger.LogError(CmdLineResources.ERROR_MissingPackageId, rawVersion);
                    return null;
                }

                if (!NuGet.SemanticVersion.TryParse(rawVersion, out packageVersion))
                {
                    this.logger.LogError(CmdLineResources.ERROR_InvalidVersion, rawVersion);
                    return null;
                }
            }
            else
            {
                packageId = argumentValue;
            }
            this.logger.LogDebug(CmdLineResources.DEBUG_ParsedReference, packageId, packageVersion);

            return new NuGetReference(packageId, packageVersion);
        }

        private bool TryParseSqaleFile(IEnumerable<ArgumentInstance> arguments, out string sqaleFilePath)
        {
            sqaleFilePath = null;
            ArgumentInstance arg = arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.SqaleXmlFile, a.Descriptor.Id));

            if (arg != null)
            {
                if (File.Exists(arg.Value))
                {
                    sqaleFilePath = arg.Value;
                    this.logger.LogDebug(CmdLineResources.DEBUG_UsingSqaleFile, sqaleFilePath);
                }
                else
                {
                    this.logger.LogError(CmdLineResources.ERROR_SqaleFileNotFound, arg.Value);
                }
            }
            return sqaleFilePath != null;
        }

    }
}
