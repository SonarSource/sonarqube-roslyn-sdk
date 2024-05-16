/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SonarQube.Common;
using SonarQube.Plugins.Common;

namespace SonarQube.Plugins.Roslyn.CommandLine
{
    public class ArgumentProcessor
    {
        #region Argument definitions

        /// <summary>
        /// Unique IDs for supported arguments that may have multiple aliases.
        /// </summary>
        private static class KeywordIds
        {
            public const string AnalyzerRef = "analyzer.ref";
            public const string SqaleXmlFile = "sqale.xml";
            public const string RuleXmlFile = "rules.xml";
            public const string AcceptLicenses = "accept.licenses";
            public const string RecurseDependencies = "recurse.dependencies";
            public const string OutputDirectory = "output.dir";
            public const string CustomNuGetRepository = "custom.nugetrepo";
            public const string Language = "language";
        }

        private static readonly IList<ArgumentDescriptor> Descriptors;

        static ArgumentProcessor()
        {
            // Initialize the set of valid descriptors.
            // To add a new argument, just add it to the list.
            Descriptors = new List<ArgumentDescriptor>
            {
                new(KeywordIds.AnalyzerRef, [ "/analyzer:", "/a:" ], required: true, allowMultiple: false, description: CmdLineResources.ArgDescription_AnalzyerRef),
                new(KeywordIds.SqaleXmlFile, [ "/sqale:" ], required: false, allowMultiple: false, description: CmdLineResources.ArgDescription_SqaleXmlFile),
                new(KeywordIds.RuleXmlFile, [ "/rules:" ], required: false, allowMultiple: false, description: CmdLineResources.ArgDescription_RuleXmlFile),
                new(KeywordIds.AcceptLicenses, [ "/acceptLicenses" ], required: false, allowMultiple: false, description: CmdLineResources.ArgDescription_AcceptLicenses, isVerb: true),
                new(KeywordIds.RecurseDependencies, [ "/recurse" ], required: false, allowMultiple: false, description: CmdLineResources.ArgDescription_RecurseDependencies, isVerb: true),
                new(KeywordIds.OutputDirectory, [ "/ouputdir:", "/o:" ], required: false, allowMultiple: false, description: CmdLineResources.ArgDescription_OutputDirectory),
                new(KeywordIds.CustomNuGetRepository, [ "/customnugetrepo:" ], required: false, allowMultiple: false, description: CmdLineResources.ArgDesciption_CustomNuGetRepo),
                new(KeywordIds.Language, [ "/language:" ], required: false, allowMultiple: false, description: CmdLineResources.ArgDesciption_Language)
            };

            Debug.Assert(Descriptors.All(d => d.Prefixes != null && d.Prefixes.Any()), "All descriptors must provide at least one prefix");
            Debug.Assert(Descriptors.Select(d => d.Id).Distinct().Count() == Descriptors.Count, "All descriptors must have a unique id");
        }

        #endregion Argument definitions

        private class NuGetReference
        {
            public NuGetReference(string packageId, NuGet.SemanticVersion version)
            {
                if (string.IsNullOrWhiteSpace(packageId))
                {
                    throw new ArgumentNullException(nameof(packageId));
                }
                PackageId = packageId;
                Version = version;
            }

            public string PackageId { get; }
            public NuGet.SemanticVersion Version { get; }
        }

        public static ProcessedArgs TryProcessArguments(string[] commandLineArgs, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var processor = new ArgumentProcessor(logger);
            return processor.Process(commandLineArgs);
        }

        private readonly ILogger logger;

        private ArgumentProcessor(ILogger logger) =>
            this.logger = logger;

        public ProcessedArgs Process(string[] commandLineArgs)
        {
            ProcessedArgs processed = null;

            // This call will fail if there are duplicate, missing, or unrecognized arguments
            CommandLineParser parser = new CommandLineParser(Descriptors, false /* don't allow unrecognized */);
            bool parsedOk = parser.ParseArguments(commandLineArgs, logger, out IEnumerable<ArgumentInstance> arguments);
            parsedOk &= TryParseAnalyzerRef(arguments, out NuGetReference analyzerRef);
            parsedOk &= TryParseSqaleArgument(arguments);
            parsedOk &= TryParseRuleFile(arguments, out string ruleFilePath);
            parsedOk &= TryParseLanguage(arguments, out string language);

            bool acceptLicense = GetLicenseAcceptance(arguments);
            bool recurseDependencies = GetRecursion(arguments);
            string outputDirectory = GetOutputDirectory(arguments);
            string nuGetRepository = GetNuGetRepository(arguments);

            if (parsedOk)
            {
                Debug.Assert(analyzerRef != null, "Expecting to have a valid analyzer reference");
                processed = new ProcessedArgs(
                    analyzerRef.PackageId,
                    analyzerRef.Version,
                    language,
                    ruleFilePath,
                    acceptLicense,
                    recurseDependencies,
                    outputDirectory,
                    nuGetRepository);
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
                    logger.LogError(CmdLineResources.ERROR_MissingPackageId, rawVersion);
                    return null;
                }

                if (!NuGet.SemanticVersion.TryParse(rawVersion, out packageVersion))
                {
                    logger.LogError(CmdLineResources.ERROR_InvalidVersion, rawVersion);
                    return null;
                }
            }
            else
            {
                packageId = argumentValue;
            }
            logger.LogDebug(CmdLineResources.DEBUG_ParsedReference, packageId, packageVersion);

            return new NuGetReference(packageId, packageVersion);
        }

        private bool TryParseSqaleArgument(IEnumerable<ArgumentInstance> arguments)
        {
            ArgumentInstance arg = arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.SqaleXmlFile, a.Descriptor.Id));

            if (arg == null)
            {
                return true;
            }

            logger.LogError(CmdLineResources.ERROR_SqaleParameterIsNotSupported);
            return false;
        }

        private bool TryParseRuleFile(IEnumerable<ArgumentInstance> arguments, out string ruleFilePath)
        {
            ruleFilePath = null;
            ArgumentInstance arg = arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.RuleXmlFile, a.Descriptor.Id));

            if (arg == null)
            {
                return true;
            }

            bool success = true;
            if (File.Exists(arg.Value))
            {
                ruleFilePath = arg.Value;
                this.logger.LogDebug(CmdLineResources.DEBUG_UsingRuleFile, ruleFilePath);
            }
            else
            {
                success = false;
                this.logger.LogError(CmdLineResources.ERROR_RuleFileNotFound, arg.Value);
            }
            return success;
        }

        private bool TryParseLanguage(IEnumerable<ArgumentInstance> arguments, out string language)
        {
            if (arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.Language, a.Descriptor.Id)) is { Value.Length: > 0 } arg)
            {
                if(arg.Value == SupportedLanguages.CSharp || arg.Value == SupportedLanguages.VisualBasic)
                {
                    language = arg.Value;
                    return true;
                }
                else
                {
                    logger.LogError(CmdLineResources.ERROR_InvalidLanguage, arg.Value);
                    language = null;
                    return false;
                }
            }
            else
            {
                language = SupportedLanguages.CSharp;
                return true;
            }
        }

        private string GetNuGetRepository(IEnumerable<ArgumentInstance> arguments)
        {
            ArgumentInstance arg = arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.CustomNuGetRepository, a.Descriptor.Id));

            return arg?.Value;
        }

        private string GetOutputDirectory(IEnumerable<ArgumentInstance> arguments)
        {
            ArgumentInstance arg = arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.OutputDirectory, a.Descriptor.Id));

            if (arg != null)
            {
                return arg.Value;
            }

            return Directory.GetCurrentDirectory();
        }

        private static bool GetLicenseAcceptance(IEnumerable<ArgumentInstance> arguments)
        {
            ArgumentInstance arg = arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.AcceptLicenses, a.Descriptor.Id));
            return arg != null;
        }

        private static bool GetRecursion(IEnumerable<ArgumentInstance> arguments)
        {
            ArgumentInstance arg = arguments.SingleOrDefault(a => ArgumentDescriptor.IdComparer.Equals(KeywordIds.RecurseDependencies, a.Descriptor.Id));
            return arg != null;
        }
    }
}