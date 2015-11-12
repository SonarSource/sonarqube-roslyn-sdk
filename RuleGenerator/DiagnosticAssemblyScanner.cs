using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.SonarQube.AnalyzerPlugins;
using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Roslyn.SonarQube
{
    /// <summary>
    /// Generates SQ specific rule metadata from a Roslyn analyser assembly
    /// </summary>
    public class DiagnosticAssemblyScanner
    {
        private readonly ILogger logger;
        private readonly IEnumerable<string> additionalSearchFolders;

        public DiagnosticAssemblyScanner(ILogger logger) : this(Enumerable.Empty<string>(), logger) { }

        public DiagnosticAssemblyScanner(IEnumerable<string> additionalSearchFolders, ILogger logger)
        {
            this.logger = logger;
            this.additionalSearchFolders = additionalSearchFolders;
        }

        /// <summary>
        /// Loads the given assembly and instantiates Roslyn diagnostic objects - i.e. existing types deriving from
        /// <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        /// <returns>empty enumerable if no diagnostics were found</returns>
        public IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticsFromAssembly(string assemblyPath, string language)
        {
            Assembly analyserAssembly = LoadAnalyzerAssembly(assemblyPath);
            IEnumerable<DiagnosticAnalyzer> analysers = null;

            Debug.Assert(String.Equals(language, LanguageNames.CSharp, StringComparison.CurrentCulture) 
                || String.Equals(language, LanguageNames.VisualBasic, StringComparison.CurrentCulture));

            if (analyserAssembly != null)
            {
                try
                {
                    analysers = InstantiateDiagnosticAnalyzers(analyserAssembly, language);

                    Debug.Assert(analysers != null);
                    if (analysers.Any())
                    {
                        logger.LogInfo(Resources.AnalyzersLoadSuccess, analysers.Count());
                    }
                    else
                    {
                        logger.LogError(Resources.NoAnalysers);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(Resources.ERR_AnalyzerInstantiationFail, analyserAssembly.FullName, ex.Message);
                }
            }

            return analysers ?? Enumerable.Empty<DiagnosticAnalyzer>();
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

            logger.LogInfo(Resources.AssemblyLoadSuccess, analyzerAssembly.FullName);
            return analyzerAssembly;
        }

        private IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticAnalyzers(Assembly analyserAssembly, string language)
        {
            Debug.Assert(analyserAssembly != null);

            ICollection<DiagnosticAnalyzer> analysers = new List<DiagnosticAnalyzer>();

            // It is assumed that analyserAssembly is valid. FileNotFoundException will be thrown if dependency resolution fails.
            foreach (Type type in analyserAssembly.GetExportedTypes())
            {
                if (!type.IsAbstract &&
                    type.IsSubclassOf(typeof(DiagnosticAnalyzer)) &&
                    DiagnosticMatchesLanguage(type, language))
                {
                    DiagnosticAnalyzer analyser = (DiagnosticAnalyzer)Activator.CreateInstance(type);
                    analysers.Add(analyser);

                    logger.LogDebug(Resources.DEBUG_AnalyserLoaded, analyser.ToString());
                }
            }

            return analysers;
        }

        private static bool DiagnosticMatchesLanguage(Type type, string language)
        {
            DiagnosticAnalyzerAttribute analyzerAttribute =
                (DiagnosticAnalyzerAttribute)Attribute.GetCustomAttribute(type, typeof(DiagnosticAnalyzerAttribute));

            return analyzerAttribute.Languages.Any(l => String.Equals(l, language, StringComparison.OrdinalIgnoreCase));
        }
    }
}