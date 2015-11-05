using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public DiagnosticAssemblyScanner(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Loads the given assembly and instantiates Roslyn diagnostic objects - i.e. existing types deriving from
        /// <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        /// <returns>empty enumerable if no diagnostics were found</returns>
        public IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticsFromAssembly(string assemblyPath, string language)
        {
            Assembly analyserAssembly = LoadAnalyzerAssembly(assemblyPath);
            IEnumerable<DiagnosticAnalyzer> analysers = Enumerable.Empty<DiagnosticAnalyzer>();

            Debug.Assert(String.Equals(language, LanguageNames.CSharp, StringComparison.CurrentCulture) 
                || String.Equals(language, LanguageNames.VisualBasic, StringComparison.CurrentCulture));

            if (analyserAssembly != null)
            {
                analysers = InstantiateDiagnosticAnalyzers(analyserAssembly, language);
            }

            logger.LogInfo(Resources.AnalysersLoadSuccess, analysers.Count());
            return analysers;
        }

        private Assembly LoadAnalyzerAssembly(string assemblyPath)
        {
            Assembly analyzerAssembly = null;
            try
            {
                analyzerAssembly = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                this.logger.LogError(Resources.AssemblyLoadError, assemblyPath, ex.Message);
                return null;
            }

            logger.LogInfo(Resources.AnalysersLoadSuccess, analyzerAssembly.FullName);
            return analyzerAssembly;
        }

        private IEnumerable<DiagnosticAnalyzer> InstantiateDiagnosticAnalyzers(Assembly analyserAssembly, string language)
        {
            Debug.Assert(analyserAssembly != null);
            ICollection<DiagnosticAnalyzer> analysers = new List<DiagnosticAnalyzer>();
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