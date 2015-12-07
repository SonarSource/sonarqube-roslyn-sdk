//-----------------------------------------------------------------------
// <copyright file="WellKnownPluginProperties.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
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
