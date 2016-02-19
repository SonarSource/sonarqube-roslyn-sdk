//-----------------------------------------------------------------------
// <copyright file="ProcessedArgs.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace SonarQube.Plugins.Roslyn.CommandLine
{
    public class ProcessedArgs
    {
        private readonly NuGetReference analyzerRef;
        private readonly string sqaleFilePath;
        private readonly string language;
        private readonly bool acceptLicenses;

        public ProcessedArgs(NuGetReference analyzerRef, string sqaleFilePath, string language, bool acceptLicenses)
        {
            if (analyzerRef == null)
            {
                throw new ArgumentNullException("analyzerRef");
            }
            SupportedLanguages.ThrowIfNotSupported(language);

            this.analyzerRef = analyzerRef;
            this.sqaleFilePath = sqaleFilePath; // can be null
            this.language = language;
            this.acceptLicenses = acceptLicenses;
        }

        public NuGetReference AnalyzerRef { get { return this.analyzerRef; } }

        public string SqaleFilePath {  get { return this.sqaleFilePath; } }

        public string Language { get { return this.language; } }

        public bool AcceptLicenses { get { return this.acceptLicenses; } }
    }
}
