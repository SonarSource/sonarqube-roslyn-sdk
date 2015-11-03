using System;
using System.Xml;
using System.Xml.Serialization;

namespace Roslyn.SonarQube
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

        [XmlElement("description")]
        public XmlCDataSection DescriptionCDATA
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                return doc.CreateCDataSection(this.Description);
            }
           
        }

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