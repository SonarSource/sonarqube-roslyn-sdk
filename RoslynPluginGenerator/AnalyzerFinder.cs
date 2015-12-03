//-----------------------------------------------------------------------
// <copyright file="AnalyzerFinder.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
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
        private readonly ILogger logger;

        public AnalyzerFinder(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.logger = logger;
        }

        public IEnumerable<DiagnosticAnalyzer> FindAnalyzers(string assemblyDirectory, string nuGetDirectory)
        {
            if (string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                throw new ArgumentNullException("assemblyDirectory");
            }
            if (string.IsNullOrWhiteSpace(nuGetDirectory))
            {
                throw new ArgumentNullException("nuGetDirectory");
            }

            IList<DiagnosticAnalyzer> analyzers = new List<DiagnosticAnalyzer>();

            using (new AssemblyResolver(logger, nuGetDirectory))
            {
                // Look in every assembly under the supplied directory to see if
                // we can find and create any analyzers
                foreach (string assemblyPath in Directory.GetFiles(assemblyDirectory, "*.dll", SearchOption.AllDirectories))
                {
                    logger.LogDebug(UIResources.AF_ProcessingAssembly, assemblyPath);
                    IEnumerable<Type> analyzerTypes = this.SafeGetAnalyzerTypes(assemblyPath) ?? Enumerable.Empty<Type>();

                    foreach (Type t in analyzerTypes)
                    {
                        DiagnosticAnalyzer analyzer = this.SafeCreateAnalyzerInstance(t);
                        if (analyzer != null)
                        {
                            analyzers.Add(analyzer);
                        }
                    }
                }
            }

            logger.LogInfo(UIResources.AF_ProcessedAssembly, analyzers.Count);
            return analyzers;
        }

        private IEnumerable<Type> SafeGetAnalyzerTypes(string assemblyFilePath)
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
            catch(ReflectionTypeLoadException ex)
            {
                this.logger.LogWarning(UIResources.AF_WARN_ExceptionFetchingTypes, ex.LoaderExceptions);
            }
            catch (Exception ex)
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
    }
}
