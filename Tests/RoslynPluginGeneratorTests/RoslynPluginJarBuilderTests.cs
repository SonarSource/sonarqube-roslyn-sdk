/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource Sàrl
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

using System.Configuration;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests;

[TestClass]
public class RoslynPluginJarBuilderTests
{
    public TestContext TestContext { get; set; }

    [DataTestMethod]
    [DataRow("cs", "cs")]
    [DataRow("vb", "vbnet")]
    public void RoslynPlugin(string language, string expectedRepository)
    {
        string testDir = TestUtils.CreateTestDirectory(TestContext, language);
        string workingDir = TestUtils.CreateTestDirectory(TestContext, language, ".working");
        string outputJarFilePath = Path.Combine(testDir, $"created.{language}.jar");
        string dummyRulesFile = TestUtils.CreateTextFile("rules.txt", testDir, "<rules />");
        string dummyZipFile = TestUtils.CreateTextFile("payload.txt", testDir, "zip");
        var manifest = new PluginManifest() { Key = "pluginkey", Description = "description", Name = "name" };
        var builder = new RoslynPluginJarBuilder(new TestLogger());
        builder.SetLanguage(language)
            .SetRepositoryKey("repo.key")
            .SetRepositoryName("repo.name")
            .SetRulesFilePath(dummyRulesFile)
            .SetPluginManifestProperties(manifest)
            .AddResourceFile(dummyZipFile, @"static\foo.zip")
            .SetJarFilePath(outputJarFilePath);
        builder.BuildJar(workingDir);

        using var checker = new ZipFileChecker(TestContext, outputJarFilePath);
        checker.AssertZipContainsFiles(
            @"META-INF\MANIFEST.MF",
            @"static\foo.zip",
            @"org\sonar\plugins\roslynsdk\configuration.xml",
            @"org\sonar\plugins\roslynsdk\rules.xml");
        checker.AssertZipDoesNotContainFiles(@"org\sonar\plugins\roslynsdk\sqale.xml");
        checker.AssertZipFileContent(@"org\sonar\plugins\roslynsdk\configuration.xml", $"""
            <?xml version="1.0" encoding="utf-8"?>
            <RoslynSdkConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <PluginKeyDifferentiator>pluginkey</PluginKeyDifferentiator>
              <RepositoryKey>repo.key</RepositoryKey>
              <RepositoryLanguage>{expectedRepository}</RepositoryLanguage>
              <RepositoryName>repo.name</RepositoryName>
              <RulesXmlResourcePath>/org/sonar/plugins/roslynsdk/rules.xml</RulesXmlResourcePath>
              <PluginProperties />
            </RoslynSdkConfiguration>
            """);
    }
}