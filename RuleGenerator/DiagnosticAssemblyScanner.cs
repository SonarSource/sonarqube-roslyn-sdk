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

        private Assembly LoadAnalyzerAssembly(string assemblyPath)
        {
            string additionalSearchFolderRawString = ProgramSettings.Default.AdditionalDependencySearchFolders;
            AssemblyResolver additionalAssemblyResolver = null;
            if (!String.IsNullOrWhiteSpace(additionalSearchFolderRawString))
            {
                IEnumerable<string> additionalSearchFolders = new List<string>(additionalSearchFolderRawString.Split(','));
                
                additionalSearchFolders = from folder in additionalSearchFolders
                    where Directory.Exists(folder) == true
                    select folder;

                if (additionalSearchFolders.Any())
                {
                    additionalAssemblyResolver = new AssemblyResolver(additionalSearchFolders.ToArray(), logger);
                }
            }

            Assembly analyzerAssembly = null;
            try
            {
                analyzerAssembly = Assembly.LoadFrom(assemblyPath);
            }
            finally
            {
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