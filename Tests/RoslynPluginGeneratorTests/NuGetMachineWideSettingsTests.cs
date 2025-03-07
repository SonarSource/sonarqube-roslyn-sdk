﻿/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Test.Common;

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
            string rootDir = TestUtils.CreateTestDirectory(TestContext);

            // Act
            IMachineWideSettings testSubject = new NuGetMachineWideSettings(rootDir);

            testSubject.Settings.Count().Should().Be(0, "Unexpected number of settings files");
        }

        [TestMethod]
        public void MachineSettings_MultipleFiles_MultipleLoaded()
        {
            // Arrange
            string baseDir = TestUtils.CreateTestDirectory(TestContext);

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

        #endregion Tests

        #region Private methods

        private static void CreateValidConfigFile(string rootDirectory, string relativeFilePath)
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
        }

        private static void AssertExpectedConfigFilesLoaded(IMachineWideSettings actual, string baseDir, params string[] relativeConfigFilePaths)
        {
            actual.Should().NotBeNull("MachineWideSettings should not be null");
            foreach (string relativePath in relativeConfigFilePaths)
            {
                string fullPath = Path.Combine(baseDir, relativePath);
                actual.Settings.Any(s => string.Equals(s.ConfigFilePath, fullPath, StringComparison.OrdinalIgnoreCase)).Should()
                    .BeTrue("Expected config file was not loaded: {0}", relativePath);
            }
            actual.Settings.Count().Should().Be(relativeConfigFilePaths.Length, "Too many config files loaded");
        }

        #endregion Private methods
    }
}