//-----------------------------------------------------------------------
// <copyright file="MavenCoordinateTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Maven;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class MavenCoordinateTests
    {
        [TestMethod]
        public void Coord_Equality()
        {
            MavenCoordinate coord1 = new MavenCoordinate();
            MavenCoordinate coord2 = new MavenCoordinate();

            // 1. Same object
            AssertCoordinatesAreEqual(coord1, coord1);

            // 2. Different empty objects
            AssertCoordinatesAreEqual(coord1, coord2);

            // 3. Same property values, same case
            coord1 = new MavenCoordinate("group1", "artifact1", "version1");
            coord2 = new MavenCoordinate("group1", "artifact1", "version1");

            AssertCoordinatesAreEqual(coord2, coord1);

            // 4. Same property values, different case
            coord1 = new MavenCoordinate("AAA", "bbb", "CCC");
            coord2 = new MavenCoordinate("aaa", "BBB", "ccc");

            AssertCoordinatesAreEqual(coord2, coord1);

            // 5. Null values
            coord1.Version = null;
            coord2.Version = null;

            AssertCoordinatesAreEqual(coord1, coord2);
        }

        [TestMethod]
        public void Coord_Inequality()
        {
            // 1. Different group id
            MavenCoordinate coord1 = new MavenCoordinate("g1", "a", "1.0");
            MavenCoordinate coord2 = new MavenCoordinate("g2", "a", "1.0");
            AssertObjectsAreNotEqual(coord1, coord2);

            // 2. Different group id
            coord1 = new MavenCoordinate("g1", "a", "1.0");
            coord2 = new MavenCoordinate("g1", "b", "1.0");
            AssertObjectsAreNotEqual(coord1, coord2);

            // 3. Different group version
            coord1 = new MavenCoordinate("g1", "a", "1.0");
            coord2 = new MavenCoordinate("g1", "a", "1.0.0");
            AssertObjectsAreNotEqual(coord1, coord2);

            // 4. Different type
            AssertObjectsAreNotEqual(coord1, "foo");

            // 5. Null value
            AssertObjectsAreNotEqual(coord1, null);
        }

        [TestMethod]
        public void Coord_ArtifactEquality()
        {
            // 1. Same object
            MavenCoordinate coord1 = new MavenCoordinate("g1", "a1", "1.0");
            bool areEqual = MavenCoordinate.IsSameArtifact(coord1, coord1);
            Assert.IsTrue(areEqual);

            // 2. Different versions, same case
            coord1 = new MavenCoordinate("g1", "a1", "1.0");
            MavenCoordinate coord2 = new MavenCoordinate("g1", "a1", "2.0");

            areEqual = MavenCoordinate.IsSameArtifact(coord1, coord2);
            Assert.IsTrue(areEqual);

            // 3. Different versions, different case
            coord1 = new MavenCoordinate("AAA", "bb", "1.0");
            coord2 = new MavenCoordinate("aaa", "BB", "2.0");

            areEqual = MavenCoordinate.IsSameArtifact(coord1, coord2);
            Assert.IsTrue(areEqual);
        }

        [TestMethod]
        public void Coord_ArtifactInequality()
        {
            // 1. Both null
            bool areEqual = MavenCoordinate.IsSameArtifact(null, null);
            Assert.IsFalse(areEqual);

            // 2. One null
            MavenCoordinate coord1 = new MavenCoordinate("g1", "a1", "1.0");
            areEqual = MavenCoordinate.IsSameArtifact(null, coord1);
            Assert.IsFalse(areEqual);

            // 3. Different group id
            coord1 = new MavenCoordinate("g1", "a1", "1.0");
            MavenCoordinate coord2 = new MavenCoordinate("g2", "a1", "1.0");

            areEqual = MavenCoordinate.IsSameArtifact(coord1, coord2);
            Assert.IsFalse(areEqual);

            // 3. Different artifact id
            coord1 = new MavenCoordinate("g1", "AA", "1.0");
            coord2 = new MavenCoordinate("g1", "BB", "1.0");

            areEqual = MavenCoordinate.IsSameArtifact(coord1, coord2);
            Assert.IsFalse(areEqual);
        }

        private void AssertCoordinatesAreEqual(MavenCoordinate obj1, MavenCoordinate obj2)
        {
            Assert.AreEqual(obj1, obj1);
            Assert.AreEqual(obj1.GetHashCode(), obj2.GetHashCode());
        }

        private void AssertObjectsAreNotEqual(MavenCoordinate subject, object other)
        {
            bool equals = subject.Equals(other);
            Assert.IsFalse(equals);
        }

    }
}
