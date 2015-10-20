using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube;
using System.IO;
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

            rules.Add(new rule()
            {
                Key = "key1",
                InternalKey = "internalKey1",
                Name="Rule1",
                Description = "description 1",
                Severity = "CRITICAL",
                Cardinality = "SINGLE",
                Status = "READY",
                Tag = "tag1"
            });

            rules.Add(new rule()
            {
                Key = "key2",
                InternalKey = "internalKey2",
                Name = "Rule2",
                Description = "description 2",
                Severity = "MAJOR",
                Cardinality = "SINGLE",
                Status = "READY",
                Tag = "tag2"
            });


            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string rulesFile = Path.Combine(testDir, "rules.xml");

            rules.Save(rulesFile);
            this.TestContext.AddResultFile(rulesFile);

            Assert.IsTrue(File.Exists(rulesFile), "Expected rules file does not exist: {0}", rulesFile);
            Rules reloaded = Rules.Load(rulesFile);

            Assert.AreEqual(2, reloaded.Count, "Unexpected number of rules in reloaded rules file");

            string expectedXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<rules xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <rule>
    <key>key1</key>
    <name>Rule1</name>
    <internalKey>internalKey1</internalKey>
    <description>description 1</description>
    <severity>CRITICAL</severity>
    <cardinality>SINGLE</cardinality>
    <status>READY</status>
    <tag>tag1</tag>
  </rule>
  <rule>
    <key>key2</key>
    <name>Rule2</name>
    <internalKey>internalKey2</internalKey>
    <description>description 2</description>
    <severity>MAJOR</severity>
    <cardinality>SINGLE</cardinality>
    <status>READY</status>
    <tag>tag2</tag>
  </rule>
</rules>";

            Assert.AreEqual(expectedXml, File.ReadAllText(rulesFile), "Unexpected XML content");
        }
    }
}
