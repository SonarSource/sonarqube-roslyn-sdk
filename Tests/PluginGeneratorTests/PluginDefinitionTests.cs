/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
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

            PluginManifest originalDefn = new PluginManifest()
            {
                Name = "name",
                Class = "class",
                Description = "description",
                Key = "key",
                Developers = "developers",
                Homepage = "homepage",
                IssueTrackerUrl = "issuetracker",
                License ="license",
                Organization = "organization",
                OrganizationUrl = "organizationurl",
                SourcesUrl ="sources",
                TermsConditionsUrl = "terms",
                Version = "version"      
            };

            originalDefn.Save(filePath);
            Assert.IsTrue(File.Exists(filePath), "File was not created: {0}", filePath);

            PluginManifest reloadedDefn = PluginManifest.Load(filePath);

            Assert.IsNotNull(reloadedDefn, "Reloaded object should not be null");
            Assert.AreEqual("name", reloadedDefn.Name, "Unexpected name");
            Assert.AreEqual("class", reloadedDefn.Class, "Unexpected class");
            Assert.AreEqual("description", reloadedDefn.Description, "Unexpected description");
            Assert.AreEqual("key", reloadedDefn.Key, "Unexpected key");

            Assert.AreEqual("issuetracker", reloadedDefn.IssueTrackerUrl, "Unexpected issue tracker url");
            Assert.AreEqual("license", reloadedDefn.License, "Unexpected license");
            Assert.AreEqual("developers", reloadedDefn.Developers, "Unexpected developers");
            Assert.AreEqual("homepage", reloadedDefn.Homepage, "Unexpected homepage");
            Assert.AreEqual("organization", reloadedDefn.Organization, "Unexpected organization");
            Assert.AreEqual("organizationurl", reloadedDefn.OrganizationUrl, "Unexpected organization url");
            Assert.AreEqual("sources", reloadedDefn.SourcesUrl, "Unexpected sources");
            Assert.AreEqual("terms", reloadedDefn.TermsConditionsUrl, "Unexpected terms");
            Assert.AreEqual("version", reloadedDefn.Version, "Unexpected version");

            this.TestContext.AddResultFile(filePath);
        }
    }
}
