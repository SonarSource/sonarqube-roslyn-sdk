//-----------------------------------------------------------------------
// <copyright file="RulesPluginBuilder.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Plugins.Common;
using SonarQube.Plugins.Maven;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SonarQube.Plugins
{
    /// <summary>
    /// Creates a SonarQube plugin that adds a new rules repository
    /// </summary>
    public class RulesPluginBuilder : PluginBuilder
    {
        private const string RulesExtensionClassName = "PluginRulesDefinition.class";
        private const string RulesResourcesRoot = "SonarQube.Plugins.Resources.Rules.";
        private const string RulesPOMResourceName = RulesResourcesRoot + "Rules.pom";

        private string language;
        private string rulesFilePath;
        private string sqaleFilePath;

        #region Public methods

        public RulesPluginBuilder(ILogger logger)
            :base(logger)
        {
        }

        public RulesPluginBuilder(IJdkWrapper jdkWrapper, IMavenArtifactHandler artifactHandler, ILogger logger)
            :base(jdkWrapper, artifactHandler, logger)
        {
        }
        
        public RulesPluginBuilder SetLanguage(string ruleLanguage)
        {
            // This is a general-purpose rule plugin builder i.e.
            // it's not limited to C# or VB, so we can only check that the 
            // supplied language isn't null/empty.
            if (string.IsNullOrWhiteSpace(ruleLanguage))
            {
                throw new ArgumentNullException("ruleLanguage");
            }
            this.language = ruleLanguage;
            return this;
        }

        public RulesPluginBuilder SetRulesFilePath(string filePath)
        {
            // The existence of the file will be checked before building
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }
            this.rulesFilePath = filePath;
            return this;
        }

        public RulesPluginBuilder SetSqaleFilePath(string filePath)
        {
            // The existence of the file will be checked before building
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }
            this.sqaleFilePath = filePath;
            return this;
        }

        #endregion

        #region Overrides

        protected override void ApplyConfiguration(string baseWorkingDirectory)
        {
            this.ValidateRulesConfiguration();

            string uniqueId = Guid.NewGuid().ToString();

            string tempDir = Utilities.CreateSubDirectory(baseWorkingDirectory, ".rules");

            this.AddRuleSources(tempDir);
            this.AddRuleJars();

            this.SetSourceCodeTokenReplacement(WellKnownSourceCodeTokens.Rule_Language, this.language);
            this.SetSourceCodeTokenReplacement("[RESOURCE_ID]", uniqueId);

            this.AddExtension(RulesExtensionClassName);

            // Add the rules and sqale files as resources
            // The files are uniquely named to avoid issues with multiple resources
            // of the same name in different jars on the classpath. This shouldn't be
            // an issue with SonarQube as plugins should be loaded in isolation from each other
            // but it simplifies testing
            Debug.Assert(!string.IsNullOrEmpty(this.rulesFilePath));
            this.AddResourceFile(this.rulesFilePath, "resources/" + uniqueId + ".rules.xml");

            if (!string.IsNullOrEmpty(this.sqaleFilePath))
            {
                this.AddResourceFile(this.sqaleFilePath, "resources/" + uniqueId + ".sqale.xml");
            }

            // Now apply the base configuration
            base.ApplyConfiguration(baseWorkingDirectory);
        }

        #endregion

        #region Private methods

        private void ValidateRulesConfiguration()
        {
            if (string.IsNullOrWhiteSpace(this.language))
            {
                throw new InvalidOperationException(UIResources.RulesBuilder_Error_RuleLanguageMustBeSpecified);
            }

            if (string.IsNullOrWhiteSpace(this.rulesFilePath))
            {
                throw new InvalidOperationException(UIResources.RulesBuilder_Error_RulesFileMustBeSpecified);
            }
            if (!File.Exists(this.rulesFilePath))
            {
                throw new FileNotFoundException(UIResources.RulesBuilder_Error_RulesFileDoesNotExists, this.rulesFilePath);
            }

            if (!string.IsNullOrWhiteSpace(this.sqaleFilePath) && !File.Exists(this.sqaleFilePath))
            {
                throw new FileNotFoundException(UIResources.RulesBuilder_Error_SqaleFileDoesNotExists, this.sqaleFilePath);
            }
        }

        private void AddRuleSources(string workingDirectory)
        {
            SourceGenerator.CreateSourceFiles(typeof(RulesPluginBuilder).Assembly, RulesResourcesRoot, workingDirectory, new Dictionary<string, string>());

            foreach (string sourceFile in Directory.GetFiles(workingDirectory, "*.java", SearchOption.AllDirectories))
            {
                this.AddSourceFile(sourceFile);
            }
        }

        private void AddRuleJars()
        {
            // Fetch and reference the required jar files
            MavenPartialPOM pom = this.ArtifactHandler.GetPOMFromResource(this.GetType().Assembly, RulesPOMResourceName);
            IEnumerable<string> jarFiles = this.ArtifactHandler.GetJarsFromPOM(pom);

            foreach (string jarFile in jarFiles)
            {
                this.AddReferencedJar(jarFile);
            }
        }

        #endregion

    }
}
