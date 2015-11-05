using Microsoft.CodeAnalysis.Diagnostics;
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
        private AppDomain currentDomain = AppDomain.CurrentDomain;
        private List<string> folderPaths = new List<string>();

        public DiagnosticAssemblyScanner(ILogger logger)
        {
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromFolder);
            this.logger = logger;
        }

        /// <summary>
        /// Loads the given assembly and extracts information about existing types deriving from 
        /// <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        /// <returns>empty enumerable if no diagnostics were found</returns>
        public IEnumerable<DiagnosticAnalyzer> ExtractDiagnosticsFromAssembly(string assemblyPath)
        {
            Assembly analyserAssembly = LoadAnalyzerAssembly(assemblyPath);
            IEnumerable<DiagnosticAnalyzer> analysers = Enumerable.Empty<DiagnosticAnalyzer>();

            if (analyserAssembly != null)
            {
                analysers = FetchDiagnosticAnalysers(analyserAssembly);
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

        private IEnumerable<DiagnosticAnalyzer> FetchDiagnosticAnalysers(Assembly analyserAssembly)
        {
            Debug.Assert(analyserAssembly != null);
            ICollection<DiagnosticAnalyzer> analysers = new List<DiagnosticAnalyzer>();

            foreach (Type type in analyserAssembly.GetExportedTypes())
            {
                if (!type.IsAbstract && type.IsSubclassOf(typeof(DiagnosticAnalyzer)))
                {
                    DiagnosticAnalyzer analyser = (DiagnosticAnalyzer)Activator.CreateInstance(type);
                    analysers.Add(analyser);

                    logger.LogDebug(Resources.DEBUG_AnalyserLoaded, analyser.ToString());
                }
            }

            return analysers;
        }

        private Assembly LoadFromFolder(object sender, ResolveEventArgs args)
        {
            // Add the assembly's own location to the folders to search in
            folderPaths.Add(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            foreach (string folderPath in folderPaths)
            {

                string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(assemblyPath) == false)
                {
                    continue;
                }
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                if (assembly != null)
                {
                    return assembly;
                }
            }

            // Default null
            return null;
        }
    }
}