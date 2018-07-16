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
                    Tags = new[] { "t1", "t2" }
                },

                new Rule()
                {
                    Key = "key2",
                    InternalKey = "internalKey2",
                    Name = "Rule2",
                    Description = @"<p>An Html <a href=""www.bing.com""> Description",
                    Severity = "MAJOR",
                    Cardinality = "SINGLE",
                    Status = "READY",
                }
            };

            string testDir = TestUtils.CreateTestDirectory(TestContext);
            string rulesFile = Path.Combine(testDir, "rules.xml");

            rules.Save(rulesFile, new TestLogger());
            TestContext.AddResultFile(rulesFile);

            Assert.IsTrue(File.Exists(rulesFile), "Expected rules file does not exist: {0}", rulesFile);
            Rules reloaded = Rules.Load(rulesFile);

            string expectedXmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<rules xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <rule>
    <key>key1</key>
    <name>Rule1</name>
    <internalKey>internalKey1</internalKey>
    <description><![CDATA[description 1]]></description>
    <severity>CRITICAL</severity>
    <cardinality>SINGLE</cardinality>
    <status>READY</status>
    <tag>t1</tag>
    <tag>t2</tag>
  </rule>
  <rule>
    <key>key2</key>
    <name>Rule2</name>
    <internalKey>internalKey2</internalKey>
    <description><![CDATA[<p>An Html <a href=""www.bing.com""> Description]]></description>
    <severity>MAJOR</severity>
    <cardinality>SINGLE</cardinality>
    <status>READY</status>
  </rule>
</rules>";

            // Save the expected XML to make comparisons using diff tools easier
            string expectedFilePath = TestUtils.CreateTextFile("expected.xml", testDir, expectedXmlContent);
            TestContext.AddResultFile(expectedFilePath);

            // Compare the serialized output
            string actualXmlContent = File.ReadAllText(rulesFile);
            Assert.AreEqual(expectedXmlContent, actualXmlContent, "Unexpected XML content");

            // Check the rule descriptions were decoded correctly
            Assert.AreEqual("description 1", reloaded[0].Description, "Description was not deserialized correctly");
            Assert.AreEqual(@"<p>An Html <a href=""www.bing.com""> Description", reloaded[1].Description, "Description was not deserialized correctly");
        }
    }
}