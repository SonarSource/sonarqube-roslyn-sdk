using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PluginGenerator
{
    public class PluginDefinition
    {
        public string Key { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string License { get; set; }
        public string OrganizationUrl { get; set; }
        public string Homepage { get; set; }
        public string Class { get; set; }
        public string SourcesUrl { get; set; }
        public string Developers { get; set; }
        public string IssueTrackerUrl { get; set; }
        public string TermsConditionsUrl { get; set; }
        public string Organization { get; set; }

        #region Serialization

        [XmlIgnore]
        public string FilePath { get; private set; }

        public static PluginDefinition Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(PluginDefinition));

            PluginDefinition defn = null;
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                defn = serializer.Deserialize(stream) as PluginDefinition;
            }
            defn.FilePath = filePath;
            return defn;
        }

        public void Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(PluginDefinition));

            using (Stream stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, this);
            }
            this.FilePath = FilePath;
        }

        #endregion
    }
}
