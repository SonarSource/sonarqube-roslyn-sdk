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
using NuGet;
using SonarQube.Plugins.Test.Common;
using System.IO;
using System.Linq;

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
            Assert.IsInstanceOfType(actualRepo, typeof(AggregateRepository));
            AggregateRepository actualAggregateRepo = (AggregateRepository)actualRepo;

            AssertExpectedPackageSources(actualAggregateRepo,
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
            Assert.IsInstanceOfType(actualRepo, typeof(AggregateRepository));
            AggregateRepository actualAggregateRepo = (AggregateRepository)actualRepo;

            AssertExpectedPackageSources(actualAggregateRepo,
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
            Assert.IsInstanceOfType(actualRepo, typeof(AggregateRepository));
            AggregateRepository actualAggregateRepo = (AggregateRepository)actualRepo;

            AssertExpectedPackageSources(actualAggregateRepo
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
            Assert.IsNull(locatedPackage, "Should have failed to locate a package");
            logger.AssertSingleWarningExists(NuGetLoggerAdapter.LogMessagePrefix, "http://bad.remote.unreachable.repo");
            logger.AssertWarningsLogged(1);
            logger.AssertErrorsLogged(0);
        }

        #endregion

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
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);            
            string fullConfigFilePath = Path.Combine(testDir, "validConfig.txt");
            File.WriteAllText(fullConfigFilePath, configXml);

            Settings settings = new NuGet.Settings(new NuGet.PhysicalFileSystem(testDir), "validConfig.txt");
            return settings;
        }

        private static void AssertExpectedPackageSources(AggregateRepository actualRepo, params string[] expectedSources)
        {
            foreach(string expectedSource in expectedSources)
            {
                Assert.IsTrue(actualRepo.Repositories.Any(r => string.Equals(r.Source, expectedSource)),
                    "Expected package source does not exist: {0}", expectedSource);
            }

            Assert.AreEqual(expectedSources.Length, actualRepo.Repositories.Count(), "Too many repositories returned");
        }

        #endregion


    }
}
