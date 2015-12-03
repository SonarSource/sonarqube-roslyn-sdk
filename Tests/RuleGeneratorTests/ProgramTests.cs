//-----------------------------------------------------------------------
// <copyright file="ProgramTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube;
using System.Linq;
using Tests.Common;
using System.IO;
using System.Collections.Generic;
using TestUtilities;

namespace RuleGeneratorTests
{
    [TestClass]
    public class ProgramTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        /// <summary>
        /// Tests that the search path validation will take a string[] and return only the valid directory paths
        /// </summary>
        [TestMethod]
        public void SearchPaths_Validation()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            string testFolder = TestUtils.CreateTestDirectory(this.TestContext);

            string testFile = Path.Combine(testFolder, "test.txt"); 
            File.WriteAllText(testFile, String.Empty);

            IEnumerable<string> testStrings = new List<string>(new string[]
            {
                null,
                "",
                "  ",
                "////",
                testFolder, // the only expected result
                testFile
            });

            // Act
            IEnumerable<string> resultStrings = Program.ParseAndValidateSearchPaths(testStrings);

            // Assert
            var expectedStrings = new List<string>(new string[] { testFolder });
            Assert.IsNotNull(resultStrings);
            Assert.AreEqual(resultStrings.Count(), 1);
            CollectionAssert.AreEquivalent(resultStrings.ToList(), expectedStrings);
        }

        #endregion
    }
}
