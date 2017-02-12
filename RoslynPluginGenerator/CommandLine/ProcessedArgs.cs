//-----------------------------------------------------------------------
// <copyright file="ProcessedArgs.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using NuGet;
using System;

namespace SonarQube.Plugins.Roslyn.CommandLine
{
    public class ProcessedArgs
    {
        private readonly string packageId;
        private readonly SemanticVersion packageVersion;
        private readonly string sqaleFilePath;
        private readonly string language;
        private readonly bool acceptLicenses;
        private readonly bool recurseDependencies;
        private readonly string outputDirectory;

        public ProcessedArgs(string packageId, SemanticVersion packageVersion, string language, string sqaleFilePath, bool acceptLicenses, bool recurseDependencies, string outputDirectory, string htmlDescriptionResourceNamespace)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException("packageId");
            }
            // Version can be null
            SupportedLanguages.ThrowIfNotSupported(language);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentNullException("outputDirectory");
            }

            this.packageId = packageId;
            this.packageVersion = packageVersion;
            this.sqaleFilePath = sqaleFilePath; // can be null
            this.language = language;
            this.acceptLicenses = acceptLicenses;
            this.recurseDependencies = recurseDependencies;
            this.outputDirectory = outputDirectory;
            this.HtmlDescriptionResourceNamespace = htmlDescriptionResourceNamespace;
        }

        public string PackageId { get { return this.packageId; } }

        public SemanticVersion PackageVersion { get { return this.packageVersion; } }

        public string SqaleFilePath {  get { return this.sqaleFilePath; } }

        public string Language { get { return this.language; } }

        public bool AcceptLicenses { get { return this.acceptLicenses; } }

        public bool RecurseDependencies { get { return this.recurseDependencies; } }

        public string OutputDirectory { get { return this.outputDirectory; } }
        public string HtmlDescriptionResourceNamespace { get; }
    }
}
