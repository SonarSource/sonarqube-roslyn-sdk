//-----------------------------------------------------------------------
// <copyright file="DiagnosticAssemblyScanner.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Searches for and instantiates Roslyn analyzers in unknown folders and/or assemblies.
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
        /// Loads all of the given assemblies and instantiates Roslyn diagnostic objects - i.e. existing types deriving from
        /// <see cref="DiagnosticAnalyzer"/>. Non-assembly files will be ignored.
        /// </summary>
        /// <returns>Enumerable with instances of DiagnosticAnalyzer from discovered assemblies</returns>
        public IEnumerable<DiagnosticAnalyzer> InstantiateDiagnostics(string language, params string[] files)
        {
            List<DiagnosticAnalyzer> analyzers = new List<DiagnosticAnalyzer>();
            foreach (string assemblyPath in files.Where(f => IsAssembly(f)))
            {
                analyzers.AddRange(InstantiateDiagnosticsFromAssembly(assemblyPath, language));
            }

            return analyzers;
        }

        private static bool IsAssembly(string filePath)
        {
            // Not expecting .winmd files to contain Roslyn analyzers
            // so we'll ignore them
            return filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Loads the given assembly and instantiates Roslyn diagnostic objects - i.e. existing types deriving from
        /// <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        /// <returns>empty enumerable if no diagnostics were found</returns>
        private IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticsFromAssembly(string assemblyPath, string language)
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
                        this.logger.LogInfo(UIResources.Scanner_AnalyzersLoadSuccess, analyzers.Count());
                    }
                    else
                    {
                        this.logger.LogError(UIResources.Scanner_NoAnalyzers);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(UIResources.Scanner_AnalyzerInstantiationFail, analyzerAssembly.FullName, ex.Message);
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
                additionalAssemblyResolver = new AssemblyResolver(this.logger, additionalSearchFolders.ToArray());
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

            this.logger.LogInfo(UIResources.Scanner_AssemblyLoadSuccess, analyzerAssembly.FullName);
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

                    this.logger.LogDebug(UIResources.Scanner_AnalyzerLoaded, analyzer.ToString());
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
