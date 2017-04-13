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

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Helper class to serialize objects to and from XML
    /// </summary>
    public static class Serializer
    {
        #region Serialization methods

        /// <summary>
        /// Save the object as XML
        /// </summary>
        public static void SaveModel<T>(T model, string fileName) where T : class
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlWriterSettings settings = new XmlWriterSettings();

            settings.CloseOutput = true;
            settings.ConformanceLevel = ConformanceLevel.Document;
            settings.Indent = true;
            settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
            settings.OmitXmlDeclaration = false;

            // Serialize to memory first to reduce the opportunity for intermittent
            // locking issues when writing the file
            using (MemoryStream stream = new MemoryStream())
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, model);
                File.WriteAllBytes(fileName, stream.ToArray());
            }
        }

        /// <summary>
        /// Loads and returns an instance of <typeparamref name="T"/> from the specified XML file
        /// </summary>
        public static T LoadModel<T>(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            XmlSerializer ser = new XmlSerializer(typeof(T));

            object o;
            using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                o = ser.Deserialize(fs);
            }

            T model = (T)o;
            return model;
        }

        #endregion Serialisation methods
    }
}
