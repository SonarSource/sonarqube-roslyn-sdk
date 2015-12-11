// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


using System.Xml.Serialization;

namespace SonarLint.XmlDescriptor
{
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