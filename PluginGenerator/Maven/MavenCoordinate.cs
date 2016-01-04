//-----------------------------------------------------------------------
// <copyright file="MavenCoordinate.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SonarQube.Plugins.Maven
{
    // TODO: consider making this class immutable (being able to change the
    // fields messes up the implementation of GetHashCode and Equals

    /// <summary>
    /// Describes a Maven artifact
    /// </summary>
    /// <remarks>This class is XML-serializable</remarks>
    public class MavenCoordinate
    {
        public MavenCoordinate()
        {
        }

        public MavenCoordinate(string groupId, string artifactId, string version)
        {
            if (string.IsNullOrWhiteSpace(groupId)) { throw new ArgumentNullException("groupId"); }
            if (string.IsNullOrWhiteSpace(artifactId)) { throw new ArgumentNullException("artifactId"); }
            if (string.IsNullOrWhiteSpace(version)) { throw new ArgumentNullException("version"); }

            this.GroupId = groupId;
            this.ArtifactId = artifactId;
            this.Version = version;
        }

        [XmlElement("groupId")]
        public string GroupId { get; set; }

        [XmlElement("artifactId")]
        public string ArtifactId { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        /// <summary>
        /// Returns true if the coordinates refer to the same group and artifact (the 
        /// versions can be the same or different), otherwise false
        /// </summary>
        public static bool IsSameArtifact(MavenCoordinate coord1, MavenCoordinate coord2)
        {
            if (coord1 == null || coord2 == null) { return false; }

            return string.Equals(coord1.GroupId, coord2.GroupId, MavenPartialPOM.PomComparisonType) &&
                string.Equals(coord1.ArtifactId, coord2.ArtifactId, MavenPartialPOM.PomComparisonType);
        }

        public override string ToString()
        {
            const string unspecified = "{null}";
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}:{1}:{2}",
                this.GroupId ?? unspecified,
                this.ArtifactId ?? unspecified,
                this.Version ?? unspecified);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, this)) { return true; }

            MavenCoordinate other = obj as MavenCoordinate;
            if (other == null) { return false; }

            return string.Equals(this.GroupId, other.GroupId, MavenPartialPOM.PomComparisonType) &&
                string.Equals(this.ArtifactId, other.ArtifactId, MavenPartialPOM.PomComparisonType) &&
                string.Equals(this.Version, other.Version, MavenPartialPOM.PomComparisonType);
        }

        public override int GetHashCode()
        {
            return (this.GroupId ?? string.Empty).ToLowerInvariant().GetHashCode() ^
                (this.ArtifactId ?? string.Empty).ToLowerInvariant().GetHashCode() ^
                (this.Version ?? string.Empty).ToLowerInvariant().GetHashCode();
        }
    }
}
