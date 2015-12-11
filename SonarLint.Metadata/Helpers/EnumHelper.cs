//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Text.RegularExpressions;

namespace SonarLint.Common.Sqale
{
    public static class EnumHelper
    {
        public static string ToSonarQubeString(this PropertyType propertyType)
        {
            var parts = propertyType.ToString().SplitCamelCase();
            return string.Join("_", parts).ToUpperInvariant();
        }

        public static string[] SplitCamelCase(this string source)
        {
            return Regex.Split(source, @"(?<!^)(?=[A-Z])");
        }

        public static string ToSonarQubeString(this SqaleSubCharacteristic subCharacteristic)
        {
            var parts = subCharacteristic.ToString().SplitCamelCase();
            return string.Join("_", parts).ToUpperInvariant();
        }

        public static DiagnosticSeverity ToDiagnosticSeverity(this Severity severity)
        {
            return severity.ToDiagnosticSeverity(IdeVisibility.Visible);
        }

        public static DiagnosticSeverity ToDiagnosticSeverity(this Severity severity,
            IdeVisibility ideVisibility)
        {
            switch (severity)
            {
                case Severity.Info:
                    return ideVisibility == IdeVisibility.Hidden ? DiagnosticSeverity.Hidden : DiagnosticSeverity.Info;
                case Severity.Minor:
                    return ideVisibility == IdeVisibility.Hidden ? DiagnosticSeverity.Hidden : DiagnosticSeverity.Warning;
                case Severity.Major:
                case Severity.Critical:
                case Severity.Blocker:
                    return DiagnosticSeverity.Warning;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}