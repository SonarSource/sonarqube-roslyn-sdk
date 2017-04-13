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