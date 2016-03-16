//-----------------------------------------------------------------------
// <copyright file="HardcodedConstantSqaleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.XmlDescriptor;
using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Generates a simple SQALE description using a constant value for each rule
    /// </summary>
    public class HardcodedConstantSqaleGenerator
    {
        private readonly ILogger logger;

        private string remediationConstantValue;

        public HardcodedConstantSqaleGenerator(ILogger logger)
        {
            this.logger = logger;
        }

        public SqaleModel GenerateSqaleData(IEnumerable<DiagnosticAnalyzer> analyzers, string remediationValue)
        {
            if (analyzers == null)
            {
                throw new ArgumentNullException("analyzers");
            }

            Debug.Assert(remediationValue.EndsWith("min"), "Expecting a remediation value in the form '15min'");
            this.remediationConstantValue = remediationValue;

            SqaleModel root = new SqaleModel();

            foreach(DiagnosticAnalyzer analyzer in analyzers)
            {
                ProcessAnalyzer(analyzer, root);
            }

            return root;
        }

        #region Private methods

        private void ProcessAnalyzer(DiagnosticAnalyzer analyzer, SqaleModel root)
        {
            foreach(DiagnosticDescriptor diagnostic in analyzer.SupportedDiagnostics)
            {
                SqaleDescriptor sqaleDescriptor = new SqaleDescriptor()
                {
                    Remediation = new SqaleRemediation()
                    {
                        RuleKey = diagnostic.Id
                    },
                    SubCharacteristic = "MAINTAINABILITY_COMPLIANCE"
                };

                sqaleDescriptor.Remediation.Properties.AddRange(new[]
                {
                    new SqaleRemediationProperty
                    {
                        Key = "remediationFunction",
                        Text = "CONSTANT_ISSUE"
                    },
                    new SqaleRemediationProperty
                    {
                        Key = "offset",
                        Value = this.remediationConstantValue,
                        Text = string.Empty
                    }
                });

                root.Sqale.Add(sqaleDescriptor);
            }
        }

        #endregion
    }
}