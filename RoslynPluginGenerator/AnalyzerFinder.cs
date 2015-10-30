using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Roslyn.SonarQube.AnalyzerPlugins
{
    public class AnalyzerFinder
    {
        private ILogger logger;

        public AnalyzerFinder(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.logger = logger;
        }

        public IEnumerable<DiagnosticAnalyzer> FindAnalyzers(string assemblyDirectory)
        {
            if (string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                throw new ArgumentNullException("assemblyDirectory");
            }

            IList<DiagnosticAnalyzer> analyzers = new List<DiagnosticAnalyzer>();

            Resolver resolver = new Resolver(assemblyDirectory);

            // Look in every assembly under the supplied directory to see if
            // we can find and create any analyzers
            foreach (string assemblyPath in Directory.GetFiles(assemblyDirectory, "*.dll", SearchOption.AllDirectories))
            {
                logger.LogDebug(UIResources.AF_ProcessingAssembly, assemblyPath);
                Type[] analyzerTypes = this.SafeGetAnalyzerTypes(assemblyPath);

                foreach (Type t in analyzerTypes)
                {
                    DiagnosticAnalyzer analyzer = this.SafeCreateAnalyzerInstance(t);
                    if (analyzer != null)
                    {
                        analyzers.Add(analyzer);
                    }
                }
            }

            logger.LogInfo(UIResources.AF_ProcessedAssembly, analyzers.Count);
            return analyzers;
        }

        private Type[] SafeGetAnalyzerTypes(string assemblyFilePath)
        {
            Type[] analyzerTypes = null;

            Assembly asm = null;
            try
            {
                asm = Assembly.LoadFile(assemblyFilePath);
            }
            catch(Exception ex)
            {
                this.logger.LogWarning(UIResources.AF_WARN_ExceptionLoadingAssembly, assemblyFilePath, ex.Message);
                return null;
            }

            try
            {
                analyzerTypes = asm.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(DiagnosticAnalyzer))).ToArray();
            }
            catch(Exception ex)
            {
                this.logger.LogWarning(UIResources.AF_WARN_ExceptionFetchingTypes, ex.Message);
                return null;
            }
            return analyzerTypes;
        }

        private DiagnosticAnalyzer SafeCreateAnalyzerInstance(Type analyzerType)
        {
            DiagnosticAnalyzer analyzer = null;
            try
            {
                analyzer = Activator.CreateInstance(analyzerType) as DiagnosticAnalyzer;
                this.logger.LogDebug(UIResources.AF_CreatedAnalyzer, analyzerType.FullName);
            }
            catch(Exception ex)
            {
                this.logger.LogWarning(UIResources.AF_WARN_ExceptionCreatingAnalyzer, analyzerType.FullName, ex.Message);
            }
            return analyzer;
        }

        private class Resolver
        {
            private string downloadDir;

            public Resolver(string path)
            {
                this.downloadDir = path;

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }

            public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            {
                Assembly asm = null;

                string[] parts = args.Name.Split(new char[] { ' ' });

                // TODO
                string fullPath = Path.Combine(this.downloadDir, parts[0].Substring(0, parts[0].Length - 1) + ".dll");

                if (File.Exists(fullPath))
                {
                    asm = Assembly.LoadFile(fullPath);
                }
                return asm;
            }
        }

    }
}
