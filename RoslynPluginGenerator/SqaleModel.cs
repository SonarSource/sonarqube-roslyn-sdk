// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
    [XmlRoot("sqale", Namespace = "")]
    public class SqaleModel
    {
        public SqaleModel()
        {
            Sqale = new List<SqaleDescriptor>();
        }
        [XmlArray("chc")]
        public List<SqaleDescriptor> Sqale { get; private set; }
    }

    [XmlType("chc")]
    public class SqaleDescriptor
    {
        public SqaleDescriptor()
        {
            Remediation = new SqaleRemediation();
        }

        [XmlElement("key")]
        public string SubCharacteristic { get; set; }

        [XmlElement("chc")]
        public SqaleRemediation Remediation { get; set; }
    }

    public class SqaleRemediation
    {
        public SqaleRemediation()
        {
            Properties = new List<SqaleRemediationProperty>();
        }

        [XmlElement("rule-key")]
        public string RuleKey { get; set; }

        [XmlElement("prop")]
        public List<SqaleRemediationProperty> Properties { get; set; }
    }

    public class SqaleRemediationProperty
    {
        [XmlElement("key")]
        public string Key { get; set; }
        [XmlElement("txt")]
        public string Text { get; set; }
        [XmlElement("val")]
        public string Value { get; set; }
    }
}