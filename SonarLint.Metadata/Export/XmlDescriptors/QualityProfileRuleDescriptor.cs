// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using SonarLint.Common;
using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
    [XmlType("rule")]
    public class QualityProfileRuleDescriptor
    {
        public QualityProfileRuleDescriptor()
        {
            RepositoryKey = "csharpsquid";
        }
        [XmlElement("repositoryKey")]
        public string RepositoryKey { get; set; }
        [XmlElement("key")]
        public string Key { get; set; }
    }
}