//-----------------------------------------------------------------------
// <copyright file="PluginProperties.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Describes a collection of Maven POM properties
    /// </summary>
    /// <remarks>This class is XML-serializable</remarks>
    public class PluginProperties : Dictionary<string, string>, IXmlSerializable
    {
        #region IXmlSerializable methods

        // Custom serialization to allow reading/writing to the following format:
        // <ContainingElement>
        //   <example.pluginKey>example</example.pluginKey>
        //   <example.pluginVersion>example</example.pluginVersion>
        //   <example.staticResourceName>example</example.staticResourceName>
        //   ...
        // </ContainingElement>
        //
        // The name of the containing element is set by containing object
        // using the XmlElement property e.g. [XmlElement("PluginProperties")]

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();

            while (reader.IsStartElement())
            {
                this.Add(reader.Name, reader.ReadElementContentAsString());
            }
            reader.ReadEndElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            foreach (string key in this.Keys)
            {
                writer.WriteElementString(key, this[key]);
            }
        }

        #endregion
    }
}