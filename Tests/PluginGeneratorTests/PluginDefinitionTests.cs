//-----------------------------------------------------------------------
// <copyright file="PluginDefinitionTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins;
using System.IO;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class PluginDefinitionTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Defn_Serialization()
        {
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = Path.Combine(testDir, "defn1.txt");

            PluginDefinition originalDefn = new PluginDefinition()
            {
                Name = "name",
                Class = "class",
                Description = "description",
                Key = "key",
                Developers = "developers",
                Homepage = "homepage",
                IssueTrackerUrl = "issuetracker",
                Language = "language",
                License ="license",
                Organization = "organization",
                OrganizationUrl = "organizationurl",
                SourcesUrl ="sources",
                TermsConditionsUrl = "terms",
                Version = "version"      
            };

            originalDefn.Save(filePath);
            Assert.IsTrue(File.Exists(filePath), "File was not created: {0}", filePath);

            PluginDefinition reloadedDefn = PluginDefinition.Load(filePath);

            Assert.IsNotNull(reloadedDefn, "Reloaded object should not be null");
            Assert.AreEqual("name", reloadedDefn.Name, "Unexpected name");
            Assert.AreEqual("class", reloadedDefn.Class, "Unexpected class");
            Assert.AreEqual("description", reloadedDefn.Description, "Unexpected description");
            Assert.AreEqual("key", reloadedDefn.Key, "Unexpected key");

            Assert.AreEqual("issuetracker", reloadedDefn.IssueTrackerUrl, "Unexpected issue tracker url");
            Assert.AreEqual("license", reloadedDefn.License, "Unexpected license");
            Assert.AreEqual("developers", reloadedDefn.Developers, "Unexpected developers");
            Assert.AreEqual("homepage", reloadedDefn.Homepage, "Unexpected homepage");
            Assert.AreEqual("language", reloadedDefn.Language, "Unexpected language");
            Assert.AreEqual("organization", reloadedDefn.Organization, "Unexpected organization");
            Assert.AreEqual("organizationurl", reloadedDefn.OrganizationUrl, "Unexpected organization url");
            Assert.AreEqual("sources", reloadedDefn.SourcesUrl, "Unexpected sources");
            Assert.AreEqual("terms", reloadedDefn.TermsConditionsUrl, "Unexpected terms");
            Assert.AreEqual("version", reloadedDefn.Version, "Unexpected version");

            this.TestContext.AddResultFile(filePath);
        }
    }
}
