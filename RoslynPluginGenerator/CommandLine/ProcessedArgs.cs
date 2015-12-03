//-----------------------------------------------------------------------
// <copyright file="ProcessedArgs.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace Roslyn.SonarQube.AnalyzerPlugins.CommandLine
{
    public class ProcessedArgs
    {
        private readonly NuGetReference analyzerRef;
        private readonly string sqaleFilePath;

        public ProcessedArgs(NuGetReference analyzerRef, string sqaleFilePath)
        {
            if (analyzerRef == null)
            {
                throw new ArgumentNullException("analyzerRef");
            }
            this.analyzerRef = analyzerRef;
            this.sqaleFilePath = sqaleFilePath; // can be null
        }

        public NuGetReference AnalyzerRef { get { return this.analyzerRef; } }

        public string SqaleFilePath {  get { return this.sqaleFilePath; } }
    }
}
