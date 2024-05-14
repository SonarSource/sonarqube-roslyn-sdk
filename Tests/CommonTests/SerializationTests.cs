/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2022 SonarSource SA
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

namespace SonarQube.Plugins.Roslyn.RuleGeneratorTests
{
    [TestClass]
    public class SerializationTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void SerializeRules()
        {
            Rules rules = new Rules
            {
                new Rule()
                {
                    Key = "key1",
                    InternalKey = "internalKey1",
                    Name = "Rule1",
                    Description = "description 1",
                    Severity = "CRITICAL",
                    Cardinality = "SINGLE",
                    Status = "READY",
                    Type = IssueType.CODE_SMELL,
                    Tags = new[] { "t1", "t2" },
                    DebtRemediationFunction = DebtRemediationFunctionType.CONSTANT_ISSUE,
                    DebtRemediationFunctionOffset = "15min"
                },

                new Rule()
                {
                    Key = "key2",
                    InternalKey = "internalKey2",
                    Name = "Rule2",
                    Description = @"<p>An Html <a href=""www.bing.com""> Description",
                    Severity = "MAJOR",
                    Cardinality = "SINGLE",
                    Type = IssueType.BUG,
                    Status = "READY",
                }
            };

            string testDir = TestUtils.CreateTestDirectory(TestContext);
            string rulesFile = Path.Combine(testDir, "rules.xml");

            rules.Save(rulesFile, new TestLogger());
            TestContext.AddResultFile(rulesFile);

            File.Exists(rulesFile).Should().BeTrue("Expected rules file does not exist: {0}", rulesFile);
            Rules reloaded = Rules.Load(rulesFile);

            string expectedXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<rules xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <rule>
    <key>key1</key>
    <name>Rule1</name>
    <internalKey>internalKey1</internalKey>
    <description><![CDATA[description 1]]></description>
    <severity>CRITICAL</severity>
    <cardinality>SINGLE</cardinality>
    <status>READY</status>
    <type>CODE_SMELL</type>
    <tag>t1</tag>
    <tag>t2</tag>
    <debtRemediationFunction>CONSTANT_ISSUE</debtRemediationFunction>
    <debtRemediationFunctionOffset>15min</debtRemediationFunctionOffset>
  </rule>
  <rule>
    <key>key2</key>
    <name>Rule2</name>
    <internalKey>internalKey2</internalKey>
    <description><![CDATA[<p>An Html <a href=""www.bing.com""> Description]]></description>
    <severity>MAJOR</severity>
    <cardinality>SINGLE</cardinality>
    <status>READY</status>
    <type>BUG</type>
  </rule>
</rules>";

            // Save the expected XML to make comparisons using diff tools easier
            string expectedFilePath = TestUtils.CreateTextFile("expected.xml", testDir, expectedXmlContent);
            TestContext.AddResultFile(expectedFilePath);

            // Compare the serialized output
            string actualXmlContent = File.ReadAllText(rulesFile);
            actualXmlContent.Should().Be(expectedXmlContent, "Unexpected XML content");

            // Check the rule descriptions were decoded correctly
            reloaded[0].Description.Should().Be("description 1", "Description was not deserialized correctly");
            reloaded[1].Description.Should().Be(@"<p>An Html <a href=""www.bing.com""> Description", "Description was not deserialized correctly");
        }
    }
}