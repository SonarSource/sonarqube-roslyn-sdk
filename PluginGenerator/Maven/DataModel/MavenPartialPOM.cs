//-----------------------------------------------------------------------
// <copyright file="MavenPartialPOM.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Maven
{
    /// <summary>
    /// XML-serializable description of a Maven POM file.
    /// NB this is a partial description of the POM that only includes
    /// the fields we need.
    /// </summary>
    [XmlRoot(ElementName = "project")]
    public class MavenPartialPOM
    {
        public const StringComparison PomComparisonType = StringComparison.OrdinalIgnoreCase;

        public const string PomNamespace = "http://maven.apache.org/POM/4.0.0";

        public MavenPartialPOM()
        {
            this.Dependencies = new List<MavenDependency>();
        }

        [XmlElement("modelVersion")]
        public string ModelVersion { get; set; }

        [XmlElement("groupId")]
        public string GroupId { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("parent")]
        public MavenCoordinate Parent { get; set; }


        [XmlElement("artifactId")]
        public string ArtifactId { get; set; }

        [XmlElement("packaging")]
        public string Packaging { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlArray("dependencies")]
        [XmlArrayItem("dependency")]
        public List<MavenDependency> Dependencies { get; set; }

        public override string ToString()
        {
            const string unspecified = "{null}";

            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}:{1}:{2}",
                this.GroupId ?? unspecified,
                this.ArtifactId ?? unspecified,
                this.Version ?? unspecified);
        }

        #region Serialization

        [XmlIgnore]
        public string FilePath { get; private set; }

        public static MavenPartialPOM Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            MavenPartialPOM data = null;
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                data = TryLoad(stream);
            }

            if (data != null)
            {
                data.FilePath = filePath;
            }

            return data;
        }

        public static MavenPartialPOM Load(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            return TryLoad(stream);
        }

        private static MavenPartialPOM TryLoad(Stream stream)
        {
            // Try to load with namespace
            XmlSerializer serializer = new XmlSerializer(typeof(MavenPartialPOM), PomNamespace);
            MavenPartialPOM data = SafeLoad(serializer, stream);

            // If that fails, try to load without the namespace (some POMs are
            // not well formed)
            if (data == null)
            {
                stream.Position = 0;
                serializer = new XmlSerializer(typeof(MavenPartialPOM));
                data = SafeLoad(serializer, stream);
            }

            return data;
        }

        private static MavenPartialPOM SafeLoad(XmlSerializer serializer, Stream stream)
        {
            MavenPartialPOM data = null;
            try
            {
                data = serializer.Deserialize(stream) as MavenPartialPOM;
            }
            catch (InvalidOperationException)
            {
                // Ignore deserialization errors
            }

            return data;
        }

        public void Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(MavenPartialPOM));

            using (Stream stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, this);
            }
            this.FilePath = FilePath;
        }

        #endregion

    }
}
