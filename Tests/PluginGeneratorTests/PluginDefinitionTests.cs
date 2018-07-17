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

using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            string testDir = TestUtils.CreateTestDirectory(TestContext);
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
            File.Exists(filePath).Should().BeTrue("File was not created: {0}", filePath);

            PluginManifest reloadedDefn = PluginManifest.Load(filePath);

            reloadedDefn.Should().NotBeNull("Reloaded object should not be null");
            reloadedDefn.Name.Should().Be("name", "Unexpected name");
            reloadedDefn.Class.Should().Be("class", "Unexpected class");
            reloadedDefn.Description.Should().Be("description", "Unexpected description");
            reloadedDefn.Key.Should().Be("key", "Unexpected key");

            reloadedDefn.IssueTrackerUrl.Should().Be("issuetracker", "Unexpected issue tracker url");
            reloadedDefn.License.Should().Be("license", "Unexpected license");
            reloadedDefn.Developers.Should().Be("developers", "Unexpected developers");
            reloadedDefn.Homepage.Should().Be("homepage", "Unexpected homepage");
            reloadedDefn.Organization.Should().Be("organization", "Unexpected organization");
            reloadedDefn.OrganizationUrl.Should().Be("organizationurl", "Unexpected organization url");
            reloadedDefn.SourcesUrl.Should().Be("sources", "Unexpected sources");
            reloadedDefn.TermsConditionsUrl.Should().Be("terms", "Unexpected terms");
            reloadedDefn.Version.Should().Be("version", "Unexpected version");

            TestContext.AddResultFile(filePath);
        }
    }
}