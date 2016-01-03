//-----------------------------------------------------------------------
// <copyright file="MavenProperties.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Maven
{
    /// <summary>
    /// Describes a collection of Maven POM properties
    /// </summary>
    /// <remarks>This class is XML-serializable</remarks>
    public class MavenProperties : StringDictionary, IXmlSerializable
    {
        #region IXmlSerializable methods

        // Custom serialization to allow reading/writing to the following format:
        //  <properties>
        //      <sonarUpdateCenter.version>1.11</sonarUpdateCenter.version>
        //      <sonarJava.version>2.4</sonarJava.version>
        //      <h2.version>1.3.172</h2.version>
        //      ...
        //  </properties>

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            reader.ReadStartElement("properties");

            while(reader.IsStartElement())
            {
                this.Add(reader.Name, reader.ReadElementContentAsString());
            }
            reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("properties");

            foreach(string key in this.Keys)
            {
                writer.WriteElementString(key, this[key]);
            }
            
            writer.WriteEndElement();
        }

        #endregion
    }
}
