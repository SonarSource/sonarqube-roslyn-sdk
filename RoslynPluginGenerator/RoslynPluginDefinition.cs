/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource SA
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

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Data class: contains the information required to create a Roslyn analyzer plugin
    /// </summary>
    internal class RoslynPluginDefinition
    {
        public string Language { get; set; }
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
        public PluginManifest Manifest { get; set; }
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