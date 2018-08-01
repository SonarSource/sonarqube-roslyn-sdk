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
