/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.XmlDescriptor;
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
        private string remediationConstantValue;

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
                SqaleDescriptor sqaleDescriptor = new SqaleDescriptor
                {
                    Remediation = new SqaleRemediation
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