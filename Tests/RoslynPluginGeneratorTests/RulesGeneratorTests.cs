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

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoslynAnalyzer11;
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

            rule1.Description.Contains(diagnostic1.Description.ToString()).Should().BeTrue("Invalid rule description");
            rule1.Description.Contains(diagnostic1.HelpLinkUri).Should().BeTrue("Invalid rule description");
            rule1.Description.Trim().StartsWith("<![CDATA").Should().BeFalse("Description should not be formatted as a CData section");

            Rule rule2 = rules.Single(r => r.Key == diagnostic2.Id);
            VerifyRule(diagnostic2, rule2);

            rule2.Description.Contains(UIResources.RuleGen_NoDescription).Should().BeTrue("Invalid rule description");
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

                rule.Tags.Should().BeNull();
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

                rule.Description.Should().Be(UIResources.RuleGen_NoDescription);
            }
        }

        #endregion Tests

        #region Checks

        private static void AssertExpectedRuleCount(int expected, Rules rules)
        {
            rules.Should().NotBeNull("Generated rules list should not be null");
            expected.Should().Be(rules.Count, "Unexpected number of rules");
        }

        private static void VerifyRule(Microsoft.CodeAnalysis.DiagnosticDescriptor diagnostic, Rule rule)
        {
            diagnostic.Id.Should().Be(rule.Key, "Invalid rule key");
            diagnostic.Id.Should().Be(rule.InternalKey, "Invalid rule internal key");
            RuleGenerator.Cardinality.Should().Be(rule.Cardinality, "Invalid rule cardinality");
            RuleGenerator.Status.Should().Be(rule.Status, "Invalid rule status");

            rule.Name.Should().Be(diagnostic.Title.ToString(), "Invalid rule name");
            rule.Tags.Should().BeNull("No tags information should be derived from the diagnostics");

            VerifyRuleValid(rule);
        }

        /// <summary>
        /// Verifies that the rule will be accepted by SonarQube validation when rendered into XML.
        /// </summary>
        private static void VerifyRuleValid(Rule rule)
        {
            rule.Key.Should().NotBeNull();
            string.IsNullOrWhiteSpace(rule.Description).Should().BeFalse();
            if (rule.Tags != null)
            {
                foreach (String tag in rule.Tags)
                {
                    string.Equals(tag, tag.ToLowerInvariant(), StringComparison.InvariantCulture).Should().BeTrue();
                }
            }
        }

        #endregion Checks
    }
}