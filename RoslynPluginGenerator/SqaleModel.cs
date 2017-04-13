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