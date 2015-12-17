// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using System.Linq;
using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
    [XmlType("chc")]
    public class SqaleDescriptor
    {
        public static SqaleDescriptor Convert(RuleDescriptors.RuleDetail ruleDetail)
        {
            return ruleDetail.SqaleDescriptor == null
                ? null
                : new SqaleDescriptor
                {
                    Remediation = new SqaleRemediation
                    {
                        Properties =
                            ruleDetail.SqaleDescriptor.Remediation.Properties.Select(
                                property => new SqaleRemediationProperty
                                {
                                    Key = property.Key,
                                    Value = property.Value,
                                    Text = property.Text
                                }).ToList(),
                        RuleKey = ruleDetail.Key
                    },
                    SubCharacteristic = ruleDetail.SqaleDescriptor.SubCharacteristic
                };
        }

        public SqaleDescriptor()
        {
            Remediation = new SqaleRemediation();
        }

        [XmlElement("key")]
        public string SubCharacteristic { get; set; }

        [XmlElement("chc")]
        public SqaleRemediation Remediation { get; set; }
    }
}