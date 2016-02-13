//-----------------------------------------------------------------------
// <copyright file="SqaleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Common.Sqale;
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

        public SqaleRoot GenerateSqaleData(IEnumerable<DiagnosticAnalyzer> analyzers, string remediationValue)
        {
            if (analyzers == null)
            {
                throw new ArgumentNullException("analyzers");
            }

            Debug.Assert(remediationValue.EndsWith("min"), "Expecting a remediation value in the form '15min'");
            this.remediationConstantValue = remediationValue;

            SqaleRoot root = new SqaleRoot();

            foreach(DiagnosticAnalyzer analyzer in analyzers)
            {
                ProcessAnalyzer(analyzer, root);
            }

            return root;
        }

        #region Private methods

        private void ProcessAnalyzer(DiagnosticAnalyzer analyzer, SqaleRoot root)
        {
            foreach(DiagnosticDescriptor diagnostic in analyzer.SupportedDiagnostics)
            {
                SqaleDescriptor sqaleDescriptor = new SqaleDescriptor()
                {
                    Remediation = new SqaleRemediation()
                    {
                        RuleKey = diagnostic.Id
                    },
                    SubCharacteristic = SqaleSubCharacteristic.MaintainabilityCompliance.ToSonarQubeString()
                };

                sqaleDescriptor.Remediation.Properties.AddRange(new[]
                {
                    new SqaleRemediationProperty
                    {
                        Key = SonarLint.RuleDescriptors.SqaleRemediationProperty.RemediationFunctionKey,
                        Text = SonarLint.RuleDescriptors.SqaleRemediationProperty.ConstantRemediationFunctionValue
                    },
                    new SqaleRemediationProperty
                    {
                        Key = SonarLint.RuleDescriptors.SqaleRemediationProperty.OffsetKey,
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