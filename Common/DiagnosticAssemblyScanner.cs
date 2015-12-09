//-----------------------------------------------------------------------
// <copyright file="DiagnosticAssemblyScanner.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarQube.Plugins.Roslyn;
using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Generates SQ specific rule metadata from a Roslyn analyser assembly
    /// </summary>
    public class DiagnosticAssemblyScanner
    {
        private readonly ILogger logger;
        private readonly IEnumerable<string> additionalSearchFolders;

        public DiagnosticAssemblyScanner(ILogger logger, params string[] additionalSearchFolders)
        {
            this.logger = logger;
            this.additionalSearchFolders = additionalSearchFolders ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Loads all assemblies in the given directory and instantiates Roslyn diagnostic objects - i.e. existing types deriving from
        /// <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        /// <returns>empty enumerable if no diagnostics were found</returns>
        public IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticsFromDirectory(string directoryPath, string language)
        {
            List<DiagnosticAnalyzer> analyzers = new List<DiagnosticAnalyzer>();

            foreach (string assemblyPath in Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories))
            {
                analyzers.AddRange(InstantiateDiagnosticsFromAssembly(assemblyPath, language));
            }
            
            if (analyzers.Any())
            {
                return analyzers;
            }
            return Enumerable.Empty<DiagnosticAnalyzer>();
        }

        /// <summary>
        /// Loads the given assembly and instantiates Roslyn diagnostic objects - i.e. existing types deriving from
        /// <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        /// <returns>empty enumerable if no diagnostics were found</returns>
        public IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticsFromAssembly(string assemblyPath, string language)
        {
            Assembly analyzerAssembly = LoadAnalyzerAssembly(assemblyPath);
            IEnumerable<DiagnosticAnalyzer> analyzers = null;

            Debug.Assert(String.Equals(language, LanguageNames.CSharp, StringComparison.CurrentCulture) 
                || String.Equals(language, LanguageNames.VisualBasic, StringComparison.CurrentCulture));

            if (analyzerAssembly != null)
            {
                try
                {
                    analyzers = InstantiateDiagnosticAnalyzers(analyzerAssembly, language);

                    Debug.Assert(analyzers != null);
                    if (analyzers.Any())
                    {
                        logger.LogInfo(Resources.Scanner_AnalyzersLoadSuccess, analyzers.Count());
                    }
                    else
                    {
                        logger.LogError(Resources.Scanner_NoAnalysers);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(Resources.Scanner_AnalyzerInstantiationFail, analyzerAssembly.FullName, ex.Message);
                }
            }

            return analyzers ?? Enumerable.Empty<DiagnosticAnalyzer>();
        }

        /// <summary>
        /// Load the assembly at the given path into memory, with the given additional assembly search directories.
        /// </summary>
        private Assembly LoadAnalyzerAssembly(string assemblyPath)
        {
            // If there were any additional assembly search directories specified in the constructor, use them
            AssemblyResolver additionalAssemblyResolver = null;
            if (additionalSearchFolders.Any())
            {
                additionalAssemblyResolver = new AssemblyResolver(logger, additionalSearchFolders.ToArray());
            }

            Assembly analyzerAssembly = null;
            try
            {
                analyzerAssembly = Assembly.LoadFrom(assemblyPath);
            }
            finally
            {
                // Dispose of the AssemblyResolver instance, if applicable
                if (additionalAssemblyResolver != null)
                {
                    additionalAssemblyResolver.Dispose();
                }
            }

            logger.LogInfo(Resources.Scanner_AssemblyLoadSuccess, analyzerAssembly.FullName);
            return analyzerAssembly;
        }

        private IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticAnalyzers(Assembly analyserAssembly, string language)
        {
            Debug.Assert(analyserAssembly != null);

            List<DiagnosticAnalyzer> analyzers = new List<DiagnosticAnalyzer>();

            // It is assumed that analyserAssembly is valid. FileNotFoundException will be thrown if dependency resolution fails.
            foreach (Type type in analyserAssembly.GetExportedTypes())
            {
                if (!type.IsAbstract &&
                    type.IsSubclassOf(typeof(DiagnosticAnalyzer)) &&
                    DiagnosticMatchesLanguage(type, language))
                {
                    DiagnosticAnalyzer analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(type);
                    analyzers.Add(analyzer);

                    logger.LogDebug(Resources.Scanner_AnalyserLoaded, analyzer.ToString());
                }
            }

            return analyzers;
        }

        private static bool DiagnosticMatchesLanguage(Type type, string language)
        {
            DiagnosticAnalyzerAttribute analyzerAttribute =
                (DiagnosticAnalyzerAttribute)Attribute.GetCustomAttribute(type, typeof(DiagnosticAnalyzerAttribute));

            return analyzerAttribute.Languages.Any(l => String.Equals(l, language, StringComparison.OrdinalIgnoreCase));
        }
    }
}
