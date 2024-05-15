/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2024 SonarSource SA
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

using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Plugins.Test.Common;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class NuGetRepositoryFactoryTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void RepoFactory_MultipleEnabledSources_RepoCreated()
        {
            // Arrange
            TestLogger logger = new TestLogger();

            // Create a valid config settings file that specifies the package sources to use
            string configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""local1_inactive"" value=""c:\inactiveCache\should.be.ignored"" />
    <add key=""local2_active"" value=""d:\active_cache"" />
    <add key=""local3_active"" value=""c:\another\active\cache"" />
  </packageSources>
  <disabledPackageSources>
    <add key=""local1_inactive"" value=""true"" />
  </disabledPackageSources>
</configuration>";

            Settings settings = CreateSettingsFromXml(configXml);

            // Act
            IPackageRepository actualRepo = NuGetRepositoryFactory.CreateRepository(settings, logger);

            // Assert
            actualRepo.Should().BeOfType<AggregateRepository>();
            AggregateRepository actualAggregateRepo = (AggregateRepository)actualRepo;

            AssertOnlyExpectedPackageSources(actualAggregateRepo,
                "d:\\active_cache",
                "c:\\another\\active\\cache");

            logger.AssertErrorsLogged(0);
            logger.AssertWarningsLogged(0);
        }

        [TestMethod]
        public void RepoFactory_OneEnabledSource_RepoCreated()
        {
            // Arrange
            TestLogger logger = new TestLogger();

            string configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""my.local"" value=""c:\active"" />
  </packageSources>
</configuration>";

            Settings settings = CreateSettingsFromXml(configXml);

            // Act
            IPackageRepository actualRepo = NuGetRepositoryFactory.CreateRepository(settings, logger);

            // Assert
            actualRepo.Should().BeOfType<AggregateRepository>();
            AggregateRepository actualAggregateRepo = (AggregateRepository)actualRepo;

            AssertOnlyExpectedPackageSources(actualAggregateRepo,
                "c:\\active");
        }

        [TestMethod]
        public void RepoFactory_NoEnabledSources_RepoCreated()
        {
            // Arrange
            TestLogger logger = new TestLogger();

            string configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""local1_inactive"" value=""c:\inactiveCache\should.be.ignored"" />
    <add key=""local2_inactive"" value=""c:\inactiveCache2\should.be.ignored"" />
    <add key=""local3_inactive"" value=""c:\inactiveCache3\should.be.ignored"" />
  </packageSources>
  <disabledPackageSources>
    <add key=""local1_inactive"" value=""true"" />
    <add key=""local2_inactive"" value=""true"" />
    <add key=""local3_inactive"" value=""true"" />
  </disabledPackageSources>
</configuration>";

            Settings settings = CreateSettingsFromXml(configXml);

            // Act
            IPackageRepository actualRepo = NuGetRepositoryFactory.CreateRepository(settings, logger);

            // Assert
            actualRepo.Should().BeOfType<AggregateRepository>();
            AggregateRepository actualAggregateRepo = (AggregateRepository)actualRepo;

            AssertOnlyExpectedPackageSources(actualAggregateRepo
                /* no packages sources so no repositories */ );
        }

        [TestMethod]
        public void RepoFactory_FailingRepo_ErrorLoggedAndSuppressed()
        {
            // Arrange
            TestLogger logger = new TestLogger();

            string configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""remote_bad"" value=""http://bad.remote.unreachable.repo"" />
  </packageSources>
</configuration>";

            Settings settings = CreateSettingsFromXml(configXml);

            // Act
            IPackageRepository actualRepo = NuGetRepositoryFactory.CreateRepository(settings, logger);
            IPackage locatedPackage = actualRepo.FindPackage("dummy.package.id"); // trying to use the bad repo should fail

            // Assert
            locatedPackage.Should().BeNull("Should have failed to locate a package");
            logger.AssertSingleWarningExists(NuGetLoggerAdapter.LogMessagePrefix, "http://bad.remote.unreachable.repo");
            logger.AssertWarningsLogged(1);
            logger.AssertErrorsLogged(0);
        }

        [TestMethod]
        public void RepoFactory_CreateRepositoryForArguments_NoCustomNuGetRepo_DefaultUsed()
        {
            // Create a valid config settings file that specifies the package sources to use
            var configXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""local1"" value=""d:\cache1"" />
    <add key=""local2"" value=""c:\cache2"" />
  </packageSources>
  <disabledPackageSources>
    <add key=""local1_inactive"" value=""true"" />
  </disabledPackageSources>
</configuration>";
            var fullConfigFilePath = WriteConfigFile(configXml);

            var settings = new ProcessedArgsBuilder("SomePackage", "SomeoutDir")
                .SetPackageVersion("0.0.1")
                .SetLanguage("cs")
                .Build();

            var logger = new TestLogger();

            var actualRepo = NuGetRepositoryFactory.CreateRepositoryForArguments(logger, settings, 
                Path.GetDirectoryName(fullConfigFilePath));

            actualRepo.Should().BeOfType<AggregateRepository>();
            AggregateRepository actualAggregateRepo = (AggregateRepository)actualRepo;

            // There might be other machine-level nuget.config settings that have been picked,
            // so we'll only check that the known package sources above were found
            AssertExpectedPackageSourcesExist(actualAggregateRepo,
                "d:\\cache1",
                "c:\\cache2");

            logger.AssertErrorsLogged(0);
            logger.AssertWarningsLogged(0);
        }

        [TestMethod]
        public void RepoFactory_CreateRepositoryForArguments_CustomNuGetRepo_Overwrites_Default()
        {
            var settings = new ProcessedArgsBuilder("SomePackage", "SomeoutDir")
                .SetCustomNuGetRepository("file:///customrepo/path")
                .SetPackageVersion("0.0.1")
                .SetLanguage("cs")
                .Build();
            var logger = new TestLogger();

            var repo = NuGetRepositoryFactory.CreateRepositoryForArguments(logger, settings, "c:\\dummy\\config\\");

            repo.Should().BeOfType<LazyLocalPackageRepository>();
            repo.Source.Should().Be("/customrepo/path");
            logger.AssertErrorsLogged(0);
            logger.AssertWarningsLogged(0);
        }

        #endregion Tests

        #region Private methods

        /// <summary>
        /// Creates a settings instance from the supplied configuration XML
        /// </summary>
        private Settings CreateSettingsFromXml(string configXml)
        {
            // The simplest way to create a NuGet.Settings instance is to load
            // from an XML file on disk

            // Note: it's best to use local package sources for enabled packages
            // i.e. references to local directories. The directories do not have
            // to exist.
            // If you reference a remote package source such as https://www.nuget.org/api/v2/
            // then the repository factory will attempt to contact the remote repo
            // (which is slow) and will fail if it cannot be reached.
            var fullConfigPath = WriteConfigFile(configXml);
            var configDir = Path.GetDirectoryName(fullConfigPath);
            var configFileName = Path.GetFileName(fullConfigPath);

            Settings settings = new NuGet.Settings(new NuGet.PhysicalFileSystem(configDir), configFileName);
            return settings;
        }

        private string WriteConfigFile(string configXml)
        {
            string testDir = TestUtils.CreateTestDirectory(TestContext);
            string fullConfigFilePath = Path.Combine(testDir, "nuget.config");
            File.WriteAllText(fullConfigFilePath, configXml);

            return fullConfigFilePath;
        }

        private static void AssertOnlyExpectedPackageSources(AggregateRepository actualRepo, params string[] expectedSources)
        {
            AssertExpectedPackageSourcesExist(actualRepo, expectedSources);
            actualRepo.Repositories.Count().Should().Be(expectedSources.Length, "Too many repositories returned");
        }

        private static void AssertExpectedPackageSourcesExist(AggregateRepository actualRepo, params string[] expectedSources)
        {
            foreach (string expectedSource in expectedSources)
            {
                actualRepo.Repositories.Any(r => string.Equals(r.Source, expectedSource)).Should()
                    .BeTrue("Expected package source does not exist: {0}", expectedSource);
            }
        }

        #endregion Private methods
    }
}