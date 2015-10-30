using ExampleAnalyzer1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube;
using System.Linq;

namespace RuleGeneratorTests
{
    [TestClass]
    public class RulesGeneratorTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void RuleGen_SimpleRules()
        {
            // Arrange
            ConfigurableAnalyzer analyzer = new ConfigurableAnalyzer();
            var diagnostic1 = analyzer.RegisterDiagnostic(key: "DiagnosticID1", description: "Some description");
            var diagnostic2 = analyzer.RegisterDiagnostic(key: "Diagnostic2", description: "");

            IRuleGenerator generator = new RuleGenerator();

            // Act
            Rules rules = generator.GenerateRules(new[] { analyzer });

            // Assert
            AssertExpectedRuleCount(2, rules);

            Rule rule1 = rules.Single(r => r.Key == diagnostic1.Id);
            VerifyRule(diagnostic1, rule1);
            Assert.AreEqual(diagnostic1.Description.ToString(), rule1.Description, "Invalid rule description");

            Rule rule2 = rules.Single(r => r.Key == diagnostic2.Id);
            VerifyRule(diagnostic2, rule2);
            Assert.AreEqual(RuleGenerator.NoDescription, rule2.Description, "Invalid rule description");
        }

        [TestMethod]
        public void RuleGen_Tags()
        {
            // Arrange
            // single tag, multiple tags, null tags, empty tag array
            ConfigurableAnalyzer analyser = new ConfigurableAnalyzer();
            analyser.RegisterDiagnostic(tags: new[] { "tag1" }, key: "oneTag");
            analyser.RegisterDiagnostic(tags: new[] { "TAG1", "tag2" }, key: "twoTags");
            analyser.RegisterDiagnostic(tags: new string[0], key: "noTags");
            analyser.RegisterDiagnostic(tags: new[] { "" }, key: "emptyTag");
            analyser.RegisterDiagnostic(tags: new[] { "tagA", "tag", "TAG" }, key: "duplicateTags");

            IRuleGenerator generator = new RuleGenerator();

            // Act
            Rules rules = generator.GenerateRules(new[] { analyser });

            // Assert
            ValidateRule(rules, "oneTag", new[] { "tag1" });
            ValidateRule(rules, "twoTags", new[] { "tag1", "tag2" });
            ValidateRule(rules, "noTags", new string[0]);
            ValidateRule(rules, "emptyTag", new string[0]);
            ValidateRule(rules, "duplicateTags", new[] { "taga", "tag" });
        }

        #endregion Tests

        #region Checks

        private static void ValidateRule(Rules rules, string expectedKey, string[] expectedTags)
        {
            Rule rule = rules.SingleOrDefault(r => r.Key == expectedKey);
            Assert.IsNotNull(rule, "No rule found with the Key " + expectedKey);
            CollectionAssert.AreEquivalent(rule.Tags, expectedTags, "Mismatch in rule tags");
        }

        private static void AssertExpectedRuleCount(int expected, Rules rules)
        {
            Assert.IsNotNull(rules, "Generated rules list should not be null");
            Assert.AreEqual(expected, rules.Count, "Unexpected number of rules");
        }

        private static void VerifyRule(Microsoft.CodeAnalysis.DiagnosticDescriptor diagnostic, Rule rule)
        {
            Assert.AreEqual(diagnostic.Id, rule.Key, "Invalid rule key");
            Assert.AreEqual(diagnostic.Id, rule.InternalKey, "Invalid rule internal key");
            Assert.AreEqual(RuleGenerator.Cardinality, rule.Cardinality, "Invalid rule cardinality");
            Assert.AreEqual(RuleGenerator.Status, rule.Status, "Invalid rule status");

            Assert.AreEqual(diagnostic.Title.ToString(), rule.Name, "Invalid rule name");
        }

        #endregion Checks
    }
}
