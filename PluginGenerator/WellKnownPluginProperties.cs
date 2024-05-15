/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2024 SonarSource SA
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

namespace SonarQube.Plugins
{
    /// <summary>
    /// Constants for well-known plugin properties that can
    /// appear in the manifest
    /// </summary>
    public static class WellKnownPluginProperties
    {
        public const string PluginName = "Plugin-Name";
        public const string Description = "Plugin-Description";
        public const string Key = "Plugin-Key";
        public const string Class = "Plugin-Class";

        public const string License = "Plugin-License";
        public const string OrganizationUrl = "Plugin-OrganizationUrl";
        public const string Version = "Plugin-Version";
        public const string Homepage = "Plugin-Homepage";
        public const string SourcesUrl = "Plugin-SourcesUrl";
        public const string Developers = "Plugin-Developers";
        public const string IssueTrackerUrl = "Plugin-IssueTrackerUrl";
        public const string TermsAndConditionsUrl = "Plugin-TermsConditionsUrl";
        public const string OrganizationName = "Plugin-Organization";
    }
}