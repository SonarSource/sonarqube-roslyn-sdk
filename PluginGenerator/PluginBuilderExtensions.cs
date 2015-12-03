//-----------------------------------------------------------------------
// <copyright file="PluginBuilderExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace Roslyn.SonarQube.PluginGenerator
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

        public static PluginBuilder SetDescription(this PluginBuilder builder, string description)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentNullException("description");
            }
            builder.SetProperty(WellKnownPluginProperties.Description, description);
            return builder;
        }

        public static PluginBuilder SetVersion(this PluginBuilder builder, string version)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentNullException("version");
            }
            builder.SetProperty(WellKnownPluginProperties.Description, version);
            return builder;
        }

        public static PluginBuilder SetClass(this PluginBuilder builder, string qualifiedClassName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (string.IsNullOrWhiteSpace(qualifiedClassName))
            {
                throw new ArgumentNullException("qualifiedClassName");
            }
            builder.SetProperty(WellKnownPluginProperties.Class, qualifiedClassName);
            return builder;
        }

        public static PluginBuilder SetProperties(this PluginBuilder builder, PluginDefinition definition)
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
