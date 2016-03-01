//-----------------------------------------------------------------------
// <copyright file="RoslynPluginDefinition.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Data class: contains the information required to create a Roslyn analyzer plugin
    /// </summary>
    public class RoslynPluginDefinition
    {
        public string Language { get; set; }
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
        public PluginManifest Manifest { get; set; }
        public string SqaleFilePath { get; set; }
        public string RulesFilePath { get; set; }

        /// <summary>
        /// Name for the embedded resource that contains the zipped analyzer assemblies
        /// </summary>
        public string StaticResourceName { get; set; }

        /// <summary>
        /// Full path to zip file containing the assemblies to be embedded in the jar as a static resource
        /// </summary>
        public string SourceZipFilePath { get; set; }
    }
}
