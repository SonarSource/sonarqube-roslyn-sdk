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

using RoslynAnalyzer11;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.Roslyn.RuleGeneratorTests
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
            TestLogger logger = new TestLogger();
            ConfigurableAnalyzer analyzer = new ConfigurableAnalyzer();
            var diagnostic1 = analyzer.RegisterDiagnostic(key: "DiagnosticID1", description: "Some description", helpLinkUri: "www.bing.com", tags: new[] { "unnecessary" });
            var diagnostic2 = analyzer.RegisterDiagnostic(key: "Diagnostic2", description: "");

            IRuleGenerator generator = new RuleGenerator(logger);

            // Act
            Rules rules = generator.GenerateRules(new[] { analyzer });

            // Assert
            AssertExpectedRuleCount(2, rules);

            Rule rule1 = rules.Single(r => r.Key == diagnostic1.Id);
            VerifyRule(diagnostic1, rule1);

            Assert.IsTrue(rule1.Description.Contains(diagnostic1.Description.ToString()), "Invalid rule description");
            Assert.IsTrue(rule1.Description.Contains(diagnostic1.HelpLinkUri), "Invalid rule description");
            Assert.IsFalse(rule1.Description.Trim().StartsWith("<![CDATA"), "Description should not be formatted as a CData section");

            Rule rule2 = rules.Single(r => r.Key == diagnostic2.Id);
            VerifyRule(diagnostic2, rule2);

            Assert.IsTrue(rule2.Description.Contains(UIResources.RuleGen_NoDescription), "Invalid rule description");
        }

        [TestMethod]
        public void CheckNoTags()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            ConfigurableAnalyzer analyzer = new ConfigurableAnalyzer();
            analyzer.RegisterDiagnostic(key: "DiagnosticID1", tags: new[] { "t1" });
            analyzer.RegisterDiagnostic(key: "DiagnosticID2", tags: new[] { "T2" });

            IRuleGenerator generator = new RuleGenerator(logger);

            // Act
            Rules rules = generator.GenerateRules(new[] { analyzer });

            // Assert
            foreach (Rule rule in rules)
            {
                VerifyRuleValid(rule);
                
                Assert.IsNull(rule.Tags);
            }
        }

        [TestMethod]
        public void RulesMustHaveDescription()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            ConfigurableAnalyzer analyzer = new ConfigurableAnalyzer();
            analyzer.RegisterDiagnostic(key: "DiagnosticID1", description: null);
            analyzer.RegisterDiagnostic(key: "DiagnosticID1", description: "");
            analyzer.RegisterDiagnostic(key: "DiagnosticID2", description: " ");

            IRuleGenerator generator = new RuleGenerator(logger);

            // Act
            Rules rules = generator.GenerateRules(new[] { analyzer });

            // Assert
            foreach (Rule rule in rules)
            {
                VerifyRuleValid(rule);

                Assert.AreEqual(rule.Description, UIResources.RuleGen_NoDescription);
            }
        }

        #endregion Tests

        #region Checks

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
            Assert.IsNull(rule.Tags, "No tags information should be derived from the diagnostics");

            VerifyRuleValid(rule);
        }

        /// <summary> 
        /// Verifies that the rule will be accepted by SonarQube validation when rendered into XML.
        /// </summary>
        private static void VerifyRuleValid(Rule rule)
        {
            Assert.IsNotNull(rule.Key);
            Assert.IsFalse(String.IsNullOrWhiteSpace(rule.Description));
            if (rule.Tags != null)
            {
                foreach (String tag in rule.Tags)
                {
                    Assert.IsTrue(String.Equals(tag, tag.ToLowerInvariant(), StringComparison.InvariantCulture));
                }
            }
        }

        #endregion Checks
    }
}
