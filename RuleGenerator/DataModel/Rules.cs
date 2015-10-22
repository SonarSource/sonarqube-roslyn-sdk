using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Roslyn.SonarQube
{
    [XmlRoot(ElementName ="rules")]
    public class Rules : List<Rule>
    {
        #region Serialization

        [XmlIgnore]
        public string FileName { get; private set; }

        /// <summary>
        /// Saves the project to the specified file as XML
        /// </summary>
        public void Save(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }
            var xmlWriter = new XmlTextWriter(fileName, Encoding.Unicode);
            xmlWriter.WriteStartElement("rules");
            foreach (rule rule in this)
            {
#pragma warning disable CC0021 // We want a <rule> tag, it just happens that we have a rule class
                xmlWriter.WriteStartElement("rule");
#pragma warning restore CC0021 

                xmlWriter.WriteElementString("name", rule.Name);
                xmlWriter.WriteElementString("key", rule.Key);
                xmlWriter.WriteElementString("severity", rule.Severity);
                xmlWriter.WriteElementString("description", rule.Description);
                xmlWriter.WriteElementString("cardinality", rule.Cardinality);
                xmlWriter.WriteElementString("status", rule.Status);
                xmlWriter.WriteElementString("internalKey", rule.InternalKey);
                if (rule.Tags != null)
                {
                    foreach (string tag in rule.Tags)
                    {
                        xmlWriter.WriteElementString("tag", tag);
                    }
                }

                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            this.FileName = fileName;

            xmlWriter.Close();
        }

        /// <summary>
        /// Loads and returns rules from the specified XML file
        /// </summary>
        public static Rules Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            Rules model = Serializer.LoadModel<Rules>(fileName);
            model.FileName = fileName;
            return model;
        }

        #endregion

    }
}
