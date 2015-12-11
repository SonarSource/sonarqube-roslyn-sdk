// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using SonarLint.Common;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
    [XmlRoot("profile", Namespace = "")]
    public class QualityProfileRoot
    {
        public QualityProfileRoot()
        {
            Rules = new List<QualityProfileRuleDescriptor>();
            Language = "cs";
            Name = "Sonar way";
        }

        [XmlElement("language")]
        public string Language { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlArray("rules")]
        public List<QualityProfileRuleDescriptor> Rules { get; private set; }
    }
}