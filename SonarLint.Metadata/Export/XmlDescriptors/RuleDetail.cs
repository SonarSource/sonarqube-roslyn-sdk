// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
    public class RuleDetail
    {
        private const string CardinalitySingle = "SINGLE";

        public static RuleDetail Convert(RuleDescriptors.RuleDetail ruleDetail)
        {
            return new RuleDetail
            {
                Key = ruleDetail.Key,
                Title = ruleDetail.Title,
                Severity = ruleDetail.Severity.ToUpper(CultureInfo.InvariantCulture),
                Description = ruleDetail.Description,
                IsActivatedByDefault = ruleDetail.IsActivatedByDefault,
                Tags = ruleDetail.Tags,
                Parameters = ruleDetail.Parameters.Select(
                    parameter =>
                        new RuleParameter
                        {
                            Type = parameter.Type,
                            Key = parameter.Key,
                            Description = parameter.Description,
                            DefaultValue = parameter.DefaultValue
                        }).ToList(),
                Cardinality = CardinalitySingle
            };
        }

        public RuleDetail()
        {
            Tags = new List<string>();
            Parameters = new List<RuleParameter>();
        }

        [XmlElement("key")]
        public string Key { get; set; }
        [XmlElement("name")]
        public string Title { get; set; }
        [XmlElement("severity")]
        public string Severity { get; set; }
        [XmlElement("cardinality")]
        public string Cardinality { get; set; }

        [XmlIgnore]
        public string Description { get; set; }
        [XmlElement("description")]
        public XmlCDataSection DescriptionCDataSection
        {
            get
            {
                return new XmlDocument().CreateCDataSection(Description);
            }
            set
            {
                Description = value == null ? "" : value.Value;
            }
        }

        [XmlElement("tag")]
        public List<string> Tags { get; private set; }

        [XmlElement("param")]
        public List<RuleParameter> Parameters { get; private set; }

        [XmlIgnore]
        public bool IsActivatedByDefault { get; set; }
    }
}