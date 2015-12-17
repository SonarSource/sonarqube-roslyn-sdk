// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
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
}