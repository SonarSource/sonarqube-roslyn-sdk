//-----------------------------------------------------------------------
// <copyright file="NuGetPackageHandlerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;
using NuGet;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class NuGetPackageHandlerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void NuGet_TestDependencyResolutionFailure()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            TestLogger logger = new TestLogger();
            NuGetPackageHandler handler = new NuGetPackageHandler(logger);

            // Act
            // Fetch a package that should fail due to pre-release dependencies
            IPackage package = handler.FetchPackage(AnalyzerPluginGenerator.NuGetPackageSource, "codeCracker", null, testDir);

            // Assert
            // No files should have been downloaded
            Assert.IsTrue(Directory.GetFiles(testDir).Length == 0);
            Assert.IsNull(package);
        }
    }
}
