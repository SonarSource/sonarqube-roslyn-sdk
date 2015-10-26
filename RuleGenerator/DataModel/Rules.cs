using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            Serializer.SaveModel(this, fileName);
            this.FileName = fileName;
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
