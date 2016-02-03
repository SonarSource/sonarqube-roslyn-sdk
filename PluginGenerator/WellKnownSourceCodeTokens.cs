//-----------------------------------------------------------------------
// <copyright file="WellKnownSourceCodeTokens.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarQube.Plugins
{
    public static class WellKnownSourceCodeTokens
    {
        public const string Core_PluginKey = "[PLUGIN_KEY]";
        public const string Core_PluginName = "[PLUGIN_NAME]";
        public const string Core_PluginPackage = "[PLUGIN_PACKAGE]";

        public const string Rule_Language = "[RULE_LANGUAGE]";
        public const string Rule_PluginId = "[RULE_REPOSITORY_ID]";
        public const string Rule_ResourceId = "[RULE_RESOURCE_ID]";
    }
}
