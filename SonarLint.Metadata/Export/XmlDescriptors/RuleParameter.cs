// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using System.Xml;
using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
    public class RuleParameter
    {
        [XmlElement("key")]
        public string Key { get; set; }
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

        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("defaultValue")]
        public string DefaultValue { get; set; }
    }
}