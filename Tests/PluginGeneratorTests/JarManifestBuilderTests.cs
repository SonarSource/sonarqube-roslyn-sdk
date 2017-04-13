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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System;
using System.IO;
using System.Linq;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class JarManifestBuilderTests
    {

        /// <summary>
        /// Maximum allowed line length in a jar
        /// </summary>
        private const int MaxLineLength = 72;

        public TestContext TestContext { get; set; }

        #region Tests methods

        [TestMethod]
        public void Manifest_NoEntries_BuildsOk()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            JarManifestBuilder builder = new JarManifestBuilder();

            string expected = @"Manifest-Version: 1.0

";
            // Act
            string filePath = builder.WriteManifest(testDir);

            // Assert
            CheckManifest(filePath, expected);            
        }

        [TestMethod]
        public void Manifest_ValidEntries_BuildsOk()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            JarManifestBuilder builder = new JarManifestBuilder();

            string expected = @"Manifest-Version: 1.0
property1: value1
PROPERTY2: value 22
PROPERTY3: Value 333

";
            // Act
            builder.SetProperty("property1", "value1");
            builder.SetProperty("PROPERTY2", "value 22");
            builder.SetProperty("PROPERTY3", "Value 333");
            string filePath = builder.WriteManifest(testDir);

            // Assert
            CheckManifest(filePath, expected);
        }
        
        [TestMethod]
        public void Manifest_ValidLongName_BuildsOk()
        {
            // Tests writing name-value pairs that exceed the allowed line limit

            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            JarManifestBuilder builder = new JarManifestBuilder();

            string expected = @"Manifest-Version: 1.0
1111111111222222222233333333334444444444555555555566666666667777777777: 
 value1
111111111122222222223333333333444444444455555555556666666666777: AAAAAAA
 AAABBBBBBBBBB

";
            // Act
            // Max length name 
            builder.SetProperty("1111111111222222222233333333334444444444555555555566666666667777777777", "value1");

            // Long name
            builder.SetProperty("111111111122222222223333333333444444444455555555556666666666777", "AAAAAAAAAABBBBBBBBBB");

            string filePath = builder.WriteManifest(testDir);

            // Assert
            CheckManifest(filePath, expected);
        }

        [TestMethod]
        public void Manifest_ValidLongValue_BuildsOk()
        {
            // Tests writing name-value pairs that exceed the allowed line limit

            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            JarManifestBuilder builder = new JarManifestBuilder();

            string expected = @"Manifest-Version: 1.0
name1: 11111111112222222222333333333344444444445555555555666666666677777
 77777
name2: 11111111112222222222333333333344444444445555555555666666666677777
 77777111111111122222222223333333333444444444455555555556666666666777777
 77771111111111222222222233333333334444444444555555555566666666667777777
 777

";
            // Act
            // Long value - splits to next line
            builder.SetProperty("name1", "1111111111222222222233333333334444444444555555555566666666667777777777");

            // Long value - splits over multiple lines
            builder.SetProperty("name2",
                "111111111122222222223333333333444444444455555555556666666666777777777711111111112222222222333333333344444444445555555555666666666677777777771111111111222222222233333333334444444444555555555566666666667777777777");

            string filePath = builder.WriteManifest(testDir);

            // Assert
            CheckManifest(filePath, expected);
        }

        [TestMethod]
        public void Manifest_InvalidCharsInName_Throws()
        {
            // Arrange
            JarManifestBuilder builder = new JarManifestBuilder();

            string[] invalidNames = {
                "", // empty
                null,
                "%",
                "1234!",
                "a b", // whitespace
                "1\r\n2" // more whitespace
            };

            // Act and assert
            foreach (string name in invalidNames)
            {
                AssertException.Expect<ArgumentException>(() => builder.SetProperty(name, "valid.property"));
            }
        }

        [TestMethod]
        public void Manifest_NameTooLong_Throws()
        {
            // Arrange
            JarManifestBuilder builder = new JarManifestBuilder();

            string validName1 = "".PadLeft(69, 'A');
            string validName2 = "".PadLeft(70, 'A');
            string invalidName = "".PadLeft(71, 'A');

            // Act
            builder.SetProperty(validName1, "69 chars");
            builder.SetProperty(validName2, "70 chars");

            AssertException.Expect<ArgumentException>(() => builder.SetProperty(invalidName, "valid.property"));
        }

        [TestMethod]
        public void Manifest_ValidNames_Succeeds()
        {
            // Arrange
            JarManifestBuilder builder = new JarManifestBuilder();

            string[] validNames = {
                "1",
                "123",
                "abc",
                "1A2",
                "-",
                "_",
                "0123456789-_",
                "abc-xyz",
                "ABC_XYZ"
            };

            // Act
            foreach (string name in validNames)
            {
                builder.SetProperty(name, "valid.property");
            }

            string manifest = builder.BuildManifest();

        }

        #endregion

        #region Private methods

        private void CheckManifest(string filePath, string expectedContent)
        {
            // Write the expected content to a file and add it to the results
            // to make manual comparisons easier
            string expectedFilePath = Path.Combine(Path.GetDirectoryName(filePath), "expected.txt");
            File.WriteAllText(expectedFilePath, expectedContent);
            this.TestContext.AddResultFile(expectedFilePath);

            Assert.IsNotNull(filePath, "Returned file path should not be null");
            Assert.IsTrue(File.Exists(filePath), "Expected file does not exist: {0}", filePath);

            string actualContent = File.ReadAllText(filePath);

            this.TestContext.AddResultFile(filePath);
            this.TestContext.WriteLine(actualContent);

            CheckManifestInvariants(filePath);

            Assert.AreEqual(expectedContent, actualContent, false, System.Globalization.CultureInfo.InvariantCulture, "Unexpected manifest content");
        }

        private static void CheckManifestInvariants(string filePath)
        {
            string[] content = File.ReadAllLines(filePath);
            Assert.AreNotEqual(0, content.Length, "Expecting content in the manifest");

            Assert.AreEqual(content[0], "Manifest-Version: 1.0", "Expecting the first line to be the manifest version");
            Assert.AreEqual(content[content.Length - 1], string.Empty, "Expecting the last line to be blank");

            Assert.IsTrue(content.All(l => l.Length <= MaxLineLength), "Lines exceed the maximum permitted length");
        }

        #endregion
    }
}
