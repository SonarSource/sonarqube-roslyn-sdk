using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Roslyn.SonarQube
{
    public class rule
    {
        [XmlElement(ElementName="key")]
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
        public string Tag { get; set; }
    }
}
