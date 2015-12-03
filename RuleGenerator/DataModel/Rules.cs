//-----------------------------------------------------------------------
// <copyright file="Rules.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------?using Roslyn.SonarQube.Common;
using Roslyn.SonarQube.Common;
using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Roslyn.SonarQube
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