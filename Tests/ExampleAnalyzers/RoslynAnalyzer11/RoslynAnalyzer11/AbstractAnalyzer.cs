//-----------------------------------------------------------------------
// <copyright file="AbstractAnalyzer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RoslynAnalyzer11
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic, "Test#")]
    public abstract class AbstractAnalyzer : DiagnosticAnalyzer
    {
    }
}
