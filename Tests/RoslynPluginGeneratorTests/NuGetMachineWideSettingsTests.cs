//-----------------------------------------------------------------------
// <copyright file="NuGetMachineWideSettingsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Test.Common;
using System;
using System.IO;
using System.Linq;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class NuGetMachineWideSettingsTests
    {
        public TestContext TestContext { get; set; }
        
        #region Tests

        [TestMethod]
        public void MachineSettings_NoConfig_NoError()
        {
            // Arrange - dir with no config settings
            string rootDir = TestUtils.CreateTestDirectory(this.TestContext);

            // Act
            IMachineWideSettings testSubject = new NuGetMachineWideSettings(rootDir);

            Assert.AreEqual(0, testSubject.Settings.Count(), "Unexpected number of settings files");
        }

        [TestMethod]
        public void MachineSettings_MultipleFiles_MultipleLoaded()
        {
            // Arrange
            string baseDir = TestUtils.CreateTestDirectory(this.TestContext);

            CreateValidConfigFile(baseDir, "NuGet\\Config\\NuGet.config"); // valid file in root dir - should be loaded
            CreateValidConfigFile(baseDir, "NuGet\\Config\\SonarQube\\sq.config"); // valid file in SQ subdir - should be loaded

            CreateValidConfigFile(baseDir, "NuGet\\Config\\SonarQube\\sq.wrongExtension");
            CreateValidConfigFile(baseDir, "NuGet\\WrongFolder\\should.not.be.loaded.config");
            CreateValidConfigFile(baseDir, "WrongFolder\\should.not.be.loaded.config");
            
            // Act
            IMachineWideSettings testSubject = new NuGetMachineWideSettings(baseDir);

            // Assert
            AssertExpectedConfigFilesLoaded(testSubject, baseDir,
                "NuGet\\Config\\NuGet.config",
                "NuGet\\Config\\SonarQube\\sq.config");
        }

        #endregion

        #region Private methods

        private static string CreateValidConfigFile(string rootDirectory, string relativeFilePath)
        {
            string fullPath = Path.Combine(rootDirectory, relativeFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
  </packageSources>
  <activePackageSource>
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </activePackageSource>
</configuration>";

            File.WriteAllText(fullPath, configXml);
            return fullPath;
        }

        private static void AssertExpectedConfigFilesLoaded(IMachineWideSettings actual, string baseDir, params string[] relativeConfigFilePaths)
        {
            Assert.IsNotNull(actual, "MachineWideSettings should not be null");
            foreach (string relativePath in relativeConfigFilePaths)
            {
                string fullPath = Path.Combine(baseDir, relativePath);
                Assert.IsTrue(actual.Settings.Any(s => string.Equals(s.ConfigFilePath, fullPath, StringComparison.OrdinalIgnoreCase)),
                    "Expected config file was not loaded: {0}", relativePath);
            }
            Assert.AreEqual(relativeConfigFilePaths.Length, actual.Settings.Count(), "Too many config files loaded");
        }

        #endregion

    }
}
