//-----------------------------------------------------------------------
// <copyright file="MavenPOMTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Maven;
using SonarQube.Plugins.Test.Common;
using System.IO;
using System.Linq;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class MavenPartialPOMTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void MavenPOM_SaveAndReload_Succeeds()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = Path.Combine(testDir, "pom1.txt");

            MavenPartialPOM originalPOM = new MavenPartialPOM()
            {
                ArtifactId = "artifact.id",
                Description = "description",
                Name = "name",
                ModelVersion = "1.0.0",
                Packaging = "jar"
            };

            originalPOM.Parent = new MavenCoordinate("parent.group.id", "parent.artifact.id", "1.0.0-parent");

            MavenDependency desc1 = new MavenDependency("group.id.1", "artifact.id.1", "1.0.0-desc1");
            MavenDependency desc2 = new MavenDependency("group.id.2", "artifact.id.2", "1.0.0-desc2");
            originalPOM.Dependencies.Add(desc1);
            originalPOM.Dependencies.Add(desc2);

            MavenCoordinate exclusion1 = new MavenCoordinate("ex-group", "ex-artifact", "1.0.0-ex");
            desc1.Exclusions.Add(exclusion1);

            originalPOM.Save(filePath);
            Assert.IsTrue(File.Exists(filePath), "File was not created: {0}", filePath);
            this.TestContext.AddResultFile(filePath);

            // Act
            MavenPartialPOM reloadedPOM = MavenPartialPOM.Load(filePath);

            // Assert
            Assert.IsNotNull(reloadedPOM, "Reloaded object should not be null");
            AssertExpectedDescriptor(reloadedPOM.Parent, "parent.group.id", "parent.artifact.id", "1.0.0-parent");

            Assert.IsNotNull(reloadedPOM.Dependencies, "Failed to reload the dependencies");
            Assert.AreEqual(2, reloadedPOM.Dependencies.Count);
            AssertExpectedDescriptor(reloadedPOM.Dependencies[0], "group.id.1", "artifact.id.1", "1.0.0-desc1");
            AssertExpectedDescriptor(reloadedPOM.Dependencies[1], "group.id.2", "artifact.id.2", "1.0.0-desc2");

            Assert.IsNotNull(reloadedPOM.Dependencies[0].Exclusions, "Exclusions were not reloaded successfully");
            AssertExpectedDescriptor(reloadedPOM.Dependencies[0].Exclusions.FirstOrDefault(), "ex-group", "ex-artifact", "1.0.0-ex");
        }

        [TestMethod]
        public void MavenPOM_LoadRealExampleWithNamespace_Succeeds()
        {
            // Arrange
            #region File content

            // The sample POM is a based on a merger of the following two real POMs:
            // https://repo1.maven.org/maven2/org/codehaus/sonar/sonar/4.5.2/sonar-4.5.2.pom
            // https://repo1.maven.org/maven2/org/codehaus/sonar/sonar-plugin-api/4.5.2/sonar-plugin-api-4.5.2.pom

            string complexPOM = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd"">

  <!-- Dummy element added for testing -->
  <parent>
    <groupId>org.codehaus.sonar.dummy</groupId>
    <artifactId>sonar</artifactId>
    <version>4.5.2</version>
  </parent>

  <modelVersion>4.0.0</modelVersion>
  <groupId>org.codehaus.sonar</groupId>
  <artifactId>sonar</artifactId>
  <packaging>pom</packaging>
  <version>4.5.2</version>
  <name>SonarQube</name>
  <url>http://www.sonarqube.org/</url>
  <description>Open source platform for continuous inspection of code quality</description>

  <modules>
    <module>sonar-application</module>
    <module>sonar-batch</module>
    <!-- other elements removed -->
  </modules>

  <organization>
    <name>SonarSource</name>
    <url>http://www.sonarsource.com</url>
  </organization>
  <inceptionYear>2009</inceptionYear>

  <issueManagement>
    <system>jira</system>
    <url>http://jira.codehaus.org/browse/SONAR</url>
  </issueManagement>

  <distributionManagement>
    <repository>
      <id>codehaus-nexus-staging</id>
      <name>Codehaus Release Repository</name>
      <url>https://nexus.codehaus.org/service/local/staging/deploy/maven2/</url>
    </repository>
    <snapshotRepository>
      <id>sonar-snapshots</id>
      <url>${sonar.snapshotRepository.url}</url>
      <uniqueVersion>false</uniqueVersion>
    </snapshotRepository>
  </distributionManagement>

  <prerequisites>
    <!-- Note that ""prerequisites"" not inherited, but used by versions-maven-plugin 1.3.1 -->
    <maven>${maven.min.version}</maven>
  </prerequisites>

  <properties>
    <sonarUpdateCenter.version>1.11</sonarUpdateCenter.version>
    <sonarJava.version>2.4</sonarJava.version>
    <h2.version>1.3.172</h2.version>
    <!-- other elements removed -->
  </properties>

  <build>
    <extensions>
      <extension>
        <groupId>org.apache.maven.wagon</groupId>
        <artifactId>wagon-webdav</artifactId>
        <version>1.0-beta-2</version>
      </extension>
    </extensions>

    <pluginManagement>
      <!-- Plugins ordered by shortname (assembly, antrun ...) -->
      <plugins>
        <plugin>
          <groupId>org.codehaus.mojo</groupId>
          <artifactId>animal-sniffer-maven-plugin</artifactId>
          <version>${version.animal-sniffer.plugin}</version>
          <configuration>
            <signature>
              <groupId>${animal-sniffer.signature.groupId}</groupId>
              <artifactId>${animal-sniffer.signature.artifactId}</artifactId>
              <version>${animal-sniffer.signature.version}</version>
            </signature>
            <skip>${skipSanityChecks}</skip>
          </configuration>
        </plugin>

      <!-- other elements removed -->
      </plugins>
    </pluginManagement>

    <plugins>
      <plugin>
        <groupId>org.codehaus.mojo</groupId>
        <artifactId>buildnumber-maven-plugin</artifactId>
        <executions>
          <execution>
            <phase>validate</phase>
            <goals>
              <goal>create</goal>
            </goals>
          </execution>
        </executions>
        <configuration>
          <doCheck>false</doCheck>
          <doUpdate>false</doUpdate>
          <getRevisionOnlyOnce>true</getRevisionOnlyOnce>
          <revisionOnScmFailure>0</revisionOnScmFailure>
        </configuration>
      </plugin>

      <!-- other elements removed -->
    </plugins>
  </build>

  <!-- Dependencies added for testing POM serialization -->
  <dependencies>
    <dependency>
      <groupId>com.google.code.gson</groupId>
      <artifactId>gson</artifactId>
      <scope>provided</scope>
    </dependency>

    <dependency>
      <groupId>org.hibernate</groupId>
      <artifactId>hibernate-annotations</artifactId>
      <exclusions>
        <exclusion>
          <groupId>org.hibernate</groupId>
          <artifactId>hibernate-core</artifactId>
        </exclusion>
      </exclusions>
    </dependency>

    <!-- unit tests -->
    <dependency>
      <groupId>org.codehaus.sonar</groupId>
      <artifactId>sonar-testing-harness</artifactId>
      <scope>test</scope>
    </dependency>
    <!-- other dependencies removed -->
  </dependencies>

  <dependencyManagement>
    <dependencies>
      <!-- SonarQube modules -->
      <dependency>
        <groupId>org.codehaus.sonar</groupId>
        <artifactId>sonar-channel</artifactId>
        <version>4.1</version>
      </dependency>
      <dependency>
        <groupId>org.codehaus.sonar</groupId>
        <artifactId>sonar-markdown</artifactId>
        <version>${project.version}</version>
      </dependency>

      <!-- other elements removed -->
      <dependency>
        <groupId>org.hibernate</groupId>
        <artifactId>hibernate-core</artifactId>
        <version>3.3.2.GA</version>
        <exclusions>
          <exclusion>
            <groupId>javax.transaction</groupId>
            <artifactId>jta</artifactId>
          </exclusion>
          <exclusion>
            <groupId>xml-apis</groupId>
            <artifactId>xml-apis</artifactId>
          </exclusion>
        </exclusions>
      </dependency>
    </dependencies>
  </dependencyManagement>

  <mailingLists>
    <mailingList>
      <name>SonarQube users mailing list</name>
      <subscribe>http://xircles.codehaus.org/projects/sonar/lists</subscribe>
      <unsubscribe>http://xircles.codehaus.org/projects/sonar/lists</unsubscribe>
      <post>user@sonar.codehaus.org</post>
      <archive>http://www.nabble.com/Sonar-f30151.html</archive>
    </mailingList>
  </mailingLists>

  <scm>
    <connection>scm:git:git@github.com:SonarSource/sonarqube.git</connection>
    <developerConnection>scm:git:git@github.com:SonarSource/sonarqube.git</developerConnection>
    <url>https://github.com/SonarSource/sonarqube</url>
    <tag>HEAD</tag>
  </scm>

  <ciManagement>
    <system>bamboo</system>
    <url>http://bamboo.ci.codehaus.org/browse/SONAR-DEF</url>
  </ciManagement>

  <licenses>
    <license>
      <name>GNU LGPL 3</name>
      <url>http://www.gnu.org/licenses/lgpl.txt</url>
      <distribution>repo</distribution>
    </license>
  </licenses>

  <!-- Developers information should not be removed as it's
  required for deployment -->

  <developers>
    <developer>
      <id>racodond</id>
      <name>David Racodon</name>
      <email>david.racodon@sonarsource.com</email>
      <organization>SonarSource</organization>
      <timezone>+1</timezone>
    </developer>
    <!-- other elements removed -->
  </developers>

  <profiles>
    <profile>
      <id>dev</id>
      <properties>
        <skipSanityChecks>true</skipSanityChecks>
        <enforcer.skip>true</enforcer.skip>
      </properties>
    </profile>
    <profile>
      <id>release</id>
      <build>
        <plugins>
          <plugin>
            <groupId>org.apache.maven.plugins</groupId>
            <artifactId>maven-javadoc-plugin</artifactId>
            <executions>
              <execution>
                <id>attach-javadocs</id>
                <goals>
                  <goal>jar</goal>
                </goals>
              </execution>
            </executions>
          </plugin>
    <!-- other elements removed -->
        </plugins>
      </build>
    </profile>

    <!-- other elements removed -->

  </profiles>

</project>";

            #endregion

            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = TestUtils.CreateTextFile("realPom.txt", testDir, complexPOM);
            this.TestContext.AddResultFile(filePath);

            // Act
            MavenPartialPOM pom = MavenPartialPOM.Load(filePath);
            string resavedFilePath = Path.Combine(testDir, "resaved.txt");
            pom.Save(resavedFilePath);
            this.TestContext.AddResultFile(resavedFilePath);

            // Assert
            Assert.IsNotNull(pom.ArtifactId);
            Assert.IsNotNull(pom.Name);
            Assert.IsNotNull(pom.ModelVersion);
            Assert.IsNotNull(pom.Packaging);
            Assert.IsNotNull(pom.Parent);
            Assert.IsNotNull(pom.Parent.GroupId);
            Assert.IsNotNull(pom.Parent.ArtifactId);
            Assert.IsNotNull(pom.Parent.Version);

            Assert.IsNotNull(pom.Dependencies);
            Assert.AreNotEqual(0, pom.Dependencies.Count, "Failed to reloaded dependencies");

            Assert.IsTrue(pom.Dependencies.TrueForAll(d => d != null && d.Exclusions != null));
            Assert.IsTrue(pom.Dependencies.Any(d => d.Exclusions.Any())); // expecting at least one dependency to have exclusions
            Assert.IsTrue(pom.Dependencies.Any(d => d.Scope != null)); // expecting at least one dependency to have a scope
        }

        [TestMethod]
        public void MavenPOM_LoadWithoutNamespace_Succeeds()
        {
            // Arrange
            #region File content

            string simplePOM = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/maven-v4_0_0.xsd"">

  <parent>
    <groupId>org.codehaus.sonar.dummy</groupId>
    <artifactId>sonar</artifactId>
    <version>4.5.2</version>
  </parent>

  <modelVersion>4.0.0</modelVersion>
  <groupId>org.codehaus.sonar</groupId>
  <artifactId>sonar</artifactId>
  <packaging>pom</packaging>
  <version>4.5.2</version>
  <name>SonarQube</name>
  <url>http://www.sonarqube.org/</url>
  <description>Open source platform for continuous inspection of code quality</description>

  <dependencies>
    <dependency>
      <groupId>com.google.code.gson</groupId>
      <artifactId>gson</artifactId>
      <scope>provided</scope>
    </dependency>

    <dependency>
      <groupId>org.hibernate</groupId>
      <artifactId>hibernate-annotations</artifactId>
      <exclusions>
        <exclusion>
          <groupId>org.hibernate</groupId>
          <artifactId>hibernate-core</artifactId>
        </exclusion>
      </exclusions>
    </dependency>

  </dependencies>
</project>";

            #endregion

            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = TestUtils.CreateTextFile("noNamespacePOM.txt", testDir, simplePOM);
            this.TestContext.AddResultFile(filePath);

            // Act
            MavenPartialPOM pom = MavenPartialPOM.Load(filePath);
            string resavedFilePath = Path.Combine(testDir, "resaved.txt");
            pom.Save(resavedFilePath);
            this.TestContext.AddResultFile(resavedFilePath);

            // Assert - minimal checks that some data was loaded
            Assert.IsNotNull(pom.ArtifactId);
            Assert.IsNotNull(pom.Name);

            Assert.IsNotNull(pom.Dependencies);
            Assert.AreNotEqual(0, pom.Dependencies.Count, "Failed to reloaded dependencies");
        }

        #endregion

        #region Checks

        private static void AssertExpectedDescriptor(IMavenCoordinate actual,
            string groupId, string artifactId, string version)
        {
            Assert.IsNotNull(actual, "Description should not be null");
            Assert.AreEqual(groupId, actual.GroupId, "Incorrect GroupId");
            Assert.AreEqual(artifactId, actual.ArtifactId, "Incorrect ArtifactId");
            Assert.AreEqual(version, actual.Version, "Incorrect Version");
        }

        #endregion
    }
}
