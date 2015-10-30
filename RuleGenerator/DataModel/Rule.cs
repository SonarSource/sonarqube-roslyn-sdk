using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Roslyn.SonarQube
{
    [XmlType(TypeName = "rule")]
    public class Rule
    {
        [XmlElement(ElementName = "key")]
        public string Key { get; set; }

        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "internalKey")]
        public string InternalKey { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "severity")]
        public string Severity { get; set; }

        [XmlElement(ElementName = "cardinality")]
        public string Cardinality { get; set; }

        [XmlElement(ElementName = "status")]
        public string Status { get; set; }
        
        [XmlElement(ElementName = "tag")]
        public string[] Tags { get; set; }

        /// <summary>
        /// Specified the culture and case when comparing rule keys
        /// </summary>
        public static StringComparison RuleKeyComparer = StringComparison.OrdinalIgnoreCase;
    }
}
