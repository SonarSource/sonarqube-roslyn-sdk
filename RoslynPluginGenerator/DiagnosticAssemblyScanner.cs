/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2022 SonarSource SA
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
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarQube.Plugins.Common;

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
            // If there were any additional assembly search directories specified in the constructor, use them
            AssemblyResolver additionalAssemblyResolver = null;
            if (additionalSearchFolders.Any())
            {
                additionalAssemblyResolver = new AssemblyResolver(logger, additionalSearchFolders.ToArray());
            }

            List<DiagnosticAnalyzer> analyzers = new List<DiagnosticAnalyzer>();

            try
            {
                foreach (string assemblyPath in files.Where(f => Utilities.IsAssemblyLibraryFileName(f)))
                {
                    analyzers.AddRange(InstantiateDiagnosticsFromAssembly(assemblyPath, language));
                }
            }
            finally
            {
                // Dispose of the AssemblyResolver instance, if applicable
                if (additionalAssemblyResolver != null)
                {
                    additionalAssemblyResolver.Dispose();
                }
            }
            return analyzers;
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

            Debug.Assert(String.Equals(language, LanguageNames.CSharp, StringComparison.Ordinal)
                || String.Equals(language, LanguageNames.VisualBasic, StringComparison.Ordinal));

            if (analyzerAssembly != null)
            {
                try
                {
                    analyzers = InstantiateDiagnosticAnalyzers(analyzerAssembly, language);

                    Debug.Assert(analyzers != null);
                    if (analyzers.Any())
                    {
                        logger.LogInfo(UIResources.Scanner_AnalyzersLoadSuccess, analyzers.Count());
                    }
                    else
                    {
                        logger.LogInfo(UIResources.Scanner_NoAnalyzers, analyzerAssembly.ToString());
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(UIResources.Scanner_AnalyzerInstantiationFail, analyzerAssembly.FullName, ex.Message);
                }
            }

            return analyzers ?? Enumerable.Empty<DiagnosticAnalyzer>();
        }

        /// <summary>
        /// Load the assembly at the given path into memory, with the given additional assembly search directories.
        /// </summary>
        private Assembly LoadAnalyzerAssembly(string assemblyPath)
        {
            Assembly analyzerAssembly;
            analyzerAssembly = Assembly.LoadFrom(assemblyPath);

            logger.LogInfo(UIResources.Scanner_AssemblyLoadSuccess, analyzerAssembly.FullName);
            return analyzerAssembly;
        }

        private IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticAnalyzers(Assembly analyserAssembly, string language)
        {
            Debug.Assert(analyserAssembly != null);

            List<DiagnosticAnalyzer> analyzers = new List<DiagnosticAnalyzer>();

            // It is assumed that analyserAssembly is valid. FileNotFoundException will be thrown if dependency resolution fails.
            foreach (Type type in analyserAssembly.GetTypes())
            {
                if (!type.IsAbstract &&
                    type.IsSubclassOf(typeof(DiagnosticAnalyzer)) &&
                    DiagnosticMatchesLanguage(type, language))
                {
                    DiagnosticAnalyzer analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(type);
                    analyzers.Add(analyzer);

                    logger.LogDebug(UIResources.Scanner_AnalyzerLoaded, analyzer.ToString());
                }
            }

            return analyzers;
        }

        private static bool DiagnosticMatchesLanguage(Type type, string language)
        {
            DiagnosticAnalyzerAttribute analyzerAttribute =
                (DiagnosticAnalyzerAttribute)Attribute.GetCustomAttribute(type, typeof(DiagnosticAnalyzerAttribute));

            // Analyzer must have a [DiagnosticAnalyzerAttribute] to be recognized as a valid analyzer
            if (analyzerAttribute == null)
            {
                return false;
            }
            return analyzerAttribute.Languages.Any(l => String.Equals(l, language, StringComparison.OrdinalIgnoreCase));
        }
    }
}