//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Common;
using SonarLint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SonarLint.Utilities
{
    public class RuleFinder
    {
        private readonly List<Type> diagnosticAnalyzers;

        public static IEnumerable<Assembly> GetPackagedRuleAssemblies()
        {
            return new[]
            {
                Assembly.LoadFrom(typeof(TestAnalyzer).Assembly.Location)
            };
        }

        public RuleFinder()
        {
            diagnosticAnalyzers = GetPackagedRuleAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(DiagnosticAnalyzer)))
                .Where(t => t.GetCustomAttributes<RuleAttribute>().Any())
                .ToList();
        }

        public IEnumerable<Type> GetAllAnalyzerTypes()
        {
            return diagnosticAnalyzers;
        }
    }
}