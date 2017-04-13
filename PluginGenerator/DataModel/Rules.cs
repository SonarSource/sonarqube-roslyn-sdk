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

using SonarQube.Plugins.Common;
using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Roslyn
{
    [XmlRoot(ElementName = "rules")]
    public class Rules : List<Rule>
    {
        #region Serialization

        [XmlIgnore]
        public string FileName { get; private set; }

        /// <summary>
        /// Saves the project to the specified file as XML
        /// </summary>
        public void Save(string fileName, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException("fileName");
            }

            this.FileName = fileName;

            Serializer.SaveModel(this, fileName);
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

        #endregion Serialization
    }
}