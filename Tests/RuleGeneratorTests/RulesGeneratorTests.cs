using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube;
using System.IO;

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

            string testDir = TestUtils.CreateTestDir(this.TestContext);
            string rulesFile = Path.Combine(testDir, "rules.xml");
            rules.Save(rulesFile);
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
