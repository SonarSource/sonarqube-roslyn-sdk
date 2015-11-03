using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tests.Common;

namespace RuleGeneratorTests
{
    [TestClass]
    public class SerializationTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void SerializeRules()
        {
            Rules rules = new Rules();

            rules.Add(new Rule()
            {
                Key = "key1",
                InternalKey = "internalKey1",
                Name = "Rule1",
                Description= "description 1",
                Severity = "CRITICAL",
                Cardinality = "SINGLE",
                Status = "READY",                
                Tags = new[] { "t1", "t2" }  
            });

            rules.Add(new Rule()
            {
                Key = "key2",
                InternalKey = "internalKey2",
                Name = "Rule2",
                Description= @"<p>An Html <a href=""www.bing.com""> Description",
                Severity = "MAJOR",
                Cardinality = "SINGLE",
                Status = "READY",
            });

            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string rulesFile = Path.Combine(testDir, "rules.xml");

            rules.Save(rulesFile, new TestLogger());
            this.TestContext.AddResultFile(rulesFile);

            Assert.IsTrue(File.Exists(rulesFile), "Expected rules file does not exist: {0}", rulesFile);
            Rules reloaded = Rules.Load(rulesFile);

            Assert.AreEqual(2, reloaded.Count, "Unexpected number of rules in reloaded rules file");

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

            string expectedFilePath = Path.ChangeExtension(rulesFile, ".expected.txt");
            File.WriteAllText(expectedFilePath, expectedXmlContent);
            this.TestContext.AddResultFile(expectedFilePath);

            string actualXmlContent = File.ReadAllText(rulesFile);
            Assert.AreEqual(expectedXmlContent, actualXmlContent, "Unexpected XML content");
        }

        [TestMethod]
        public void TagsMustBeLowercase()
        {
            // Arrange
            Rules rules = new Rules();

            rules.Add(new Rule()
            {
                Key = "key1",
                InternalKey = "internalKey1",
                Name = "Rule1",
                Description = "description 1",
                Severity = "CRITICAL",
                Cardinality = "SINGLE",
                Status = "READY",
                Tags = new[] { "T1", "t2" }
            });

            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string rulesFile = Path.Combine(testDir, "rules.xml");

            // Act & Assert
            TestUtils.AssertExceptionIsThrown<InvalidOperationException>(
                () => rules.Save(rulesFile, new TestLogger()));
        }
    }
}
