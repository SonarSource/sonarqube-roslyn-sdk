/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource SA
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

using System;
using System.Xml;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Roslyn
{
    [XmlType(TypeName = "rule")]
    public class Rule
    {
        /// <summary>
        /// Use this property to set the rule description. HTML formatting is supported.
        /// </summary>
        [XmlIgnore]
        public string Description { get; set; }

        [XmlElement(ElementName = "key")]
        public string Key { get; set; }

        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "internalKey")]
        public string InternalKey { get; set; }

        /// <summary>
        /// Returns the description formatted as an HTML CData section for serialization purposes.
        /// </summary>
        /// <remarks>It is expected that the description will contain HTML formatting. This is serialized in
        /// a CData section to preserve the formatting.
        /// Note: the XMLSerializer requires a public getter and setter to be able to serialize a property.</remarks>
        [XmlElement("description")]
        public XmlCDataSection DescriptionAsCDATA
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                return doc.CreateCDataSection(Description);
            }
            set => Description = value.Value;
        }

        [XmlElement(ElementName = "severity")]
        public string Severity { get; set; }

        [XmlElement(ElementName = "cardinality")]
        public string Cardinality { get; set; }

        [XmlElement(ElementName = "status")]
        public string Status { get; set; }

        [XmlElement(ElementName = "type")]
        public IssueType Type { get; set; }

        [XmlElement(ElementName = "tag")]
        public string[] Tags { get; set; }

        [XmlIgnore]
        public DebtRemediationFunctionType DebtRemediationFunction { get; set; }

        /// <summary>
        /// Do not read/write this property directly - use <see cref="DebtRemediationFunction"/> instead.
        /// </summary>
        /// <remarks>This property is a hack that allows us to use an enum to specify
        /// debt remediation function, but to have nothing written to the XML if the
        /// the value is not specified. There are other ways to do this, but they required
        /// much more code.</remarks>
        [XmlElement(ElementName = "debtRemediationFunction")]
        public string DebtRemediationFunction_DoNotUse_ForSerializationOnly
        {
            get
            {
                if (DebtRemediationFunction == DebtRemediationFunctionType.Unspecified)
                {
                    return null;
                }
                return DebtRemediationFunction.ToString();
            }
            set
            {
                this.DebtRemediationFunction = (DebtRemediationFunctionType)Enum.Parse(typeof(DebtRemediationFunctionType), value);
            }
        }

        [XmlElement(ElementName = "debtRemediationFunctionOffset")]
        public string DebtRemediationFunctionOffset { get; set; }

        /// <summary>
        /// Specified the culture and case when comparing rule keys
        /// </summary>
        public static readonly StringComparison RuleKeyComparer = StringComparison.OrdinalIgnoreCase;
    }
}