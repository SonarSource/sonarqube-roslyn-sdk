using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube;
using System.IO;
using Tests.Common;

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
            ExampleAnalyzer1.ExampleAnalyzer1Analyzer analyzer1 = new ExampleAnalyzer1.ExampleAnalyzer1Analyzer();

            IRuleGenerator generator = new RuleGenerator();

            // Act
            Rules rules = generator.GenerateRules(new[] { analyzer1 });

            // Assert
            AssertExpectedRuleCount(1, rules);

            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string rulesFile = Path.Combine(testDir, "rules.xml");
            rules.Save(rulesFile);
            this.TestContext.AddResultFile(rulesFile);
        }

        [TestMethod]
        public void RuleGen_Tags()
        {
            // Arrange
            // single tag, multiple tags, null tags, empty tag array
            var oneTag = new string[] { "tag1" };
            ExampleAnalyzer1.ExampleAnalyzer1Analyzer oneTagAnalyzer = new ExampleAnalyzer1.ExampleAnalyzer1Analyzer(oneTag);

            var multipleTags = new string[] { "tag1", "tag2" };
            ExampleAnalyzer1.ExampleAnalyzer1Analyzer multiTagAnalyzer = new ExampleAnalyzer1.ExampleAnalyzer1Analyzer(multipleTags);
            
            ExampleAnalyzer1.ExampleAnalyzer1Analyzer nullTagAnalyzer = new ExampleAnalyzer1.ExampleAnalyzer1Analyzer(null);

            var emptyTags = new string[0] { };
            ExampleAnalyzer1.ExampleAnalyzer1Analyzer emptyTagAnalyzer = new ExampleAnalyzer1.ExampleAnalyzer1Analyzer(emptyTags);

            IRuleGenerator generator = new RuleGenerator();

            // Act
            Rules oneTagRules = generator.GenerateRules(new[] { oneTagAnalyzer });
            Rules multiTagRules = generator.GenerateRules(new[] { multiTagAnalyzer });
            Rules nullTagRules = generator.GenerateRules(new[] { nullTagAnalyzer });
            Rules emptyTagRules = generator.GenerateRules(new[] { emptyTagAnalyzer });

            // Assert
            AssertExpectedRuleCount(1, oneTagRules);
            AssertExpectedRuleCount(1, multiTagRules);
            AssertExpectedRuleCount(1, nullTagRules);
            AssertExpectedRuleCount(1, emptyTagRules);

            CollectionAssert.AreEqual(oneTag, oneTagRules[0].Tags);
            CollectionAssert.AreEqual(multipleTags, multiTagRules[0].Tags);
            CollectionAssert.AreEqual(null, nullTagRules[0].Tags);
            CollectionAssert.AreEqual(null, emptyTagRules[0].Tags);

            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string rulesFile = Path.Combine(testDir, "rules.xml");
            oneTagRules.Save(rulesFile);
            this.TestContext.AddResultFile(rulesFile);
        }

        #endregion

        #region Checks

        private static void AssertExpectedRuleCount(int expected, Rules rules)
        {
            Assert.IsNotNull(rules, "Generated rules list should not be null");
            Assert.AreEqual(expected, rules.Count, "Unexpected number of rules");
        }

        #endregion
    }
}
