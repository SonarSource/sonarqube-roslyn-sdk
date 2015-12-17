//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SonarLint.Common.Sqale;
using SonarLint.Common;
using SonarLint.RuleDescriptors;

namespace SonarLint.Utilities
{
    public static class RuleDetailBuilder
    {
        public static IEnumerable<RuleDetail> GetAllRuleDetails()
        {
            return new RuleFinder().GetAllAnalyzerTypes().Select(GetRuleDetail);
        }

        private static RuleDetail GetRuleDetail(Type analyzerType)
        {
            var rule = analyzerType.GetCustomAttributes<RuleAttribute>().Single();

            var ruleDetail = new RuleDetail
            {
                Key = rule.Key,
                Title = rule.Title,
                Severity = rule.Severity.ToString(),
                IdeSeverity = (int)rule.Severity.ToDiagnosticSeverity(),
                IsActivatedByDefault = rule.IsActivatedByDefault,
                Description = "Full HTML description"
            };

            GetParameters(analyzerType, ruleDetail);
            GetTags(analyzerType, ruleDetail);
            GetSqale(analyzerType, ruleDetail);

            return ruleDetail;
        }

        private static void GetSqale(Type analyzerType, RuleDetail ruleDetail)
        {
            var sqaleRemediation = analyzerType.GetCustomAttributes<SqaleRemediationAttribute>().FirstOrDefault();

            if (sqaleRemediation == null)
            {
                ruleDetail.SqaleDescriptor = null;
                return;
            }

            var sqaleSubCharacteristic = analyzerType.GetCustomAttributes<SqaleSubCharacteristicAttribute>().First();
            var sqaleDescriptor = new SqaleDescriptor
            {
                SubCharacteristic = sqaleSubCharacteristic.SubCharacteristic.ToSonarQubeString()
            };
            var constantRemediation = sqaleRemediation as SqaleConstantRemediationAttribute;
            if (constantRemediation == null)
            {
                ruleDetail.SqaleDescriptor = sqaleDescriptor;
                return;
            }

            sqaleDescriptor.Remediation.Properties.AddRange(new[]
            {
                new SqaleRemediationProperty
                {
                    Key = SqaleRemediationProperty.RemediationFunctionKey,
                    Text = SqaleRemediationProperty.ConstantRemediationFunctionValue
                },
                new SqaleRemediationProperty
                {
                    Key = SqaleRemediationProperty.OffsetKey,
                    Value = constantRemediation.Value,
                    Text = string.Empty
                }
            });

            ruleDetail.SqaleDescriptor = sqaleDescriptor;
        }

        private static void GetTags(Type analyzerType, RuleDetail ruleDetail)
        {
            var tags = analyzerType.GetCustomAttributes<TagsAttribute>().FirstOrDefault();
            if (tags != null)
            {
                ruleDetail.Tags.AddRange(tags.Tags);
            }
        }

        private static void GetParameters(Type analyzerType, RuleDetail ruleDetail)
        {
            var parameters = analyzerType.GetProperties()
                .Select(p => p.GetCustomAttributes<RuleParameterAttribute>().SingleOrDefault());

            foreach (var ruleParameter in parameters
                .Where(attribute => attribute != null))
            {
                ruleDetail.Parameters.Add(
                    new RuleParameter
                    {
                        DefaultValue = ruleParameter.DefaultValue,
                        Description = ruleParameter.Description,
                        Key = ruleParameter.Key,
                        Type = ruleParameter.Type.ToSonarQubeString()
                    });
            }
        }
    }
}
