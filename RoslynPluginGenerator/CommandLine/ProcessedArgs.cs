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

using System;
using NuGet;

namespace SonarQube.Plugins.Roslyn.CommandLine
{
    public class ProcessedArgs
    {
        public ProcessedArgs(string packageId, SemanticVersion packageVersion, string language, string ruleFilePath,
            bool acceptLicenses, bool recurseDependencies, string outputDirectory, string customNuGetRepository, bool clearTempNugetCache = false)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }
            // Version can be null
            SupportedLanguages.ThrowIfNotSupported(language);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentNullException(nameof(outputDirectory));
            }

            PackageId = packageId;
            PackageVersion = packageVersion;
            RuleFilePath = ruleFilePath;
            Language = language;
            AcceptLicenses = acceptLicenses;
            RecurseDependencies = recurseDependencies;
            OutputDirectory = outputDirectory;
            CustomNuGetRepository = customNuGetRepository;
            ClearTempNugetCache = clearTempNugetCache;
        }

        public string PackageId { get; }

        public SemanticVersion PackageVersion { get; }

        public string RuleFilePath { get; }

        public string Language { get; }

        public bool AcceptLicenses { get; }

        public bool RecurseDependencies { get; }

        public string OutputDirectory { get; }

		public string CustomNuGetRepository { get; }

        public bool ClearTempNugetCache { get; }
    }
}