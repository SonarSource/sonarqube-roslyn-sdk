//-----------------------------------------------------------------------
// <copyright file="PluginBuilderExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace SonarQube.Plugins
{
    /// <summary>
    /// Extensions to simplify building plugins
    /// </summary>
    public static class PluginBuilderExtensions
    {
        public static PluginBuilder SetPluginKey(this PluginBuilder builder, string key)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            builder.SetProperty(WellKnownPluginProperties.Key, key);
            return builder;
        }

        public static PluginBuilder SetPluginName(this PluginBuilder builder, string name)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }
            builder.SetProperty(WellKnownPluginProperties.PluginName, name);
            return builder;
        }

        public static PluginBuilder SetProperties(this PluginBuilder builder, PluginManifest definition)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }

            SetNonNullManifestProperty(WellKnownPluginProperties.License, definition.License, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.OrganizationUrl, definition.OrganizationUrl, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.Version, definition.Version, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.Homepage, definition.Homepage, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.SourcesUrl, definition.SourcesUrl, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.Developers, definition.Developers, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.IssueTrackerUrl, definition.IssueTrackerUrl, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.TermsAndConditionsUrl, definition.TermsConditionsUrl, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.OrganizationName, definition.Organization, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.PluginName, definition.Name, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.Description, definition.Description, builder);
            SetNonNullManifestProperty(WellKnownPluginProperties.Key, definition.Key, builder);

            return builder;
        }
        
        private static void SetNonNullManifestProperty(string property, string value, PluginBuilder pluginBuilder)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                pluginBuilder.SetProperty(property, value);
            }
        }

    }
}
