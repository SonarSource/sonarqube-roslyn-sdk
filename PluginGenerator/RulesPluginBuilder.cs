//-----------------------------------------------------------------------
// <copyright file="RulesPluginBuilder.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace SonarQube.Plugins
{
    public class RulesPluginBuilder
    {
        private const string RulesExtensionClassName = "PluginRulesDefinition.class";
        private const string RulesResourcesRoot = "SonarQube.Plugins.Resources.Rules.";

        private readonly IJdkWrapper jdkWrapper;
        private readonly ILogger logger;

        public RulesPluginBuilder(ILogger logger)
            :this(new JdkWrapper(), logger)
        {
        }

        public RulesPluginBuilder(IJdkWrapper jdkWrapper, ILogger logger)
        {
            if (jdkWrapper == null)
            {
                throw new ArgumentNullException("param");
            }

            this.jdkWrapper = jdkWrapper;
            this.logger = logger;
        }

        public void GeneratePlugin(PluginDefinition definition, string rulesFilePath, string fullJarFilePath)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            if (string.IsNullOrWhiteSpace(rulesFilePath))
            {
                throw new ArgumentNullException("rulesFilePath");
            }
            if (string.IsNullOrWhiteSpace(fullJarFilePath))
            {
                throw new ArgumentNullException("fullJarFilePath");
            }

            if (!File.Exists(rulesFilePath))
            {
                throw new FileNotFoundException(UIResources.Gen_Error_RulesFileDoesNotExists, rulesFilePath);
            }
            if (!this.jdkWrapper.IsJdkInstalled())
            {
                throw new InvalidOperationException(UIResources.JarB_JDK_NotInstalled);
            }

            if (File.Exists(fullJarFilePath))
            {
                this.logger.LogWarning(UIResources.Gen_ExistingJarWillBeOvewritten);
            }

            PluginBuilder builder = new PluginBuilder(jdkWrapper, logger);
            ConfigureBuilder(builder, definition, rulesFilePath, null);


            builder.SetJarFilePath(fullJarFilePath);
            builder.Build();
        }

        public static void ConfigureBuilder(PluginBuilder builder, PluginDefinition definition, string rulesFilePath, string sqaleFilePath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }
            if (string.IsNullOrWhiteSpace(rulesFilePath))
            {
                throw new ArgumentNullException("rulesFilePath");
            }

            if (!File.Exists(rulesFilePath))
            {
                throw new FileNotFoundException(UIResources.Gen_Error_RulesFileDoesNotExists, rulesFilePath);
            }

            if (!string.IsNullOrEmpty(sqaleFilePath) && !File.Exists(sqaleFilePath))
            {
                throw new FileNotFoundException(UIResources.Gen_Error_SqaleFileDoesNotExists, sqaleFilePath);
            }

            // TODO: consider moving; not specific to rules plugins
            ValidateDefinition(definition);

            // Temp folder which resources will be unpacked into
            string tempWorkingDir = Path.Combine(Path.GetTempPath(), ".plugins", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempWorkingDir);

            DoConfigureBuilder(builder, definition, rulesFilePath, sqaleFilePath, tempWorkingDir);
        }

        private static void ValidateDefinition(PluginDefinition definition)
        {
            // TODO
            CheckPropertyIsSet(definition.Language, "Language");

            CheckPropertyIsSet(definition.Key, "Key");
            CheckPropertyIsSet(definition.Name, "Name");
        }

        private static void CheckPropertyIsSet(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    UIResources.Error_MissingProperty, name));
            }
        }

        private static void DoConfigureBuilder(PluginBuilder builder, PluginDefinition definition, string rulesFilePath, string sqaleFilePath, string workingFolder)
        {
            AddRuleSources(workingFolder, builder);
            ConfigureSourceFileReplacements(definition, builder);
            builder.AddExtension(RulesExtensionClassName);

            AddRuleJars(workingFolder, builder);

            // Add the rules and sqale files as resources
            builder.AddResourceFile(rulesFilePath, "resources/rules.xml");

            if (!string.IsNullOrEmpty(sqaleFilePath))
            {
                builder.AddResourceFile(rulesFilePath, "resources/sqale.xml");
            }

            // TODO: consider moving - not specific to the rules plugin
            builder.SetProperties(definition);
        }

        private static void AddRuleSources(string workingDirectory, PluginBuilder builder)
        {
            SourceGenerator.CreateSourceFiles(typeof(RulesPluginBuilder).Assembly, RulesResourcesRoot, workingDirectory, new Dictionary<string, string>());

            foreach (string sourceFile in Directory.GetFiles(workingDirectory, "*.java", SearchOption.AllDirectories))
            {
                builder.AddSourceFile(sourceFile);
            }
        }

        private static void ConfigureSourceFileReplacements(PluginDefinition definition, PluginBuilder builder)
        {
            builder.SetSourceCodeTokenReplacement(WellKnownSourceCodeTokens.Rule_Language, definition.Language);
        }

        private static void AddRuleJars(string workingDirectory, PluginBuilder builder)
        {
            // Unpack and reference the required jar files
            SourceGenerator.UnpackReferencedJarFiles(typeof(RulesPluginBuilder).Assembly, RulesResourcesRoot, workingDirectory);
            foreach (string jarFile in Directory.GetFiles(workingDirectory, "*.jar"))
            {
                builder.AddReferencedJar(jarFile);
            }
        }
    }
}
