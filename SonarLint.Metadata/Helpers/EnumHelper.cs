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
        public static string[] SplitCamelCase(this string source)
        {
            return Regex.Split(source, @"(?<!^)(?=[A-Z])");
        }

        public static string ToSonarQubeString(this SqaleSubCharacteristic subCharacteristic)
        {
            var parts = subCharacteristic.ToString().SplitCamelCase();
            return string.Join("_", parts).ToUpperInvariant();
        }
    }
}