using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.SonarQube.PluginGenerator.NuGet;
using System.Collections.Generic;
using System.IO;
using Tests.Common;

namespace Roslyn.SonarQube.PluginGeneratorTests
{
    [TestClass]
    public class NuGetDataModelTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void NuGet_Serialization()
        {
            // Arrange
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = Path.Combine(testDir, "Diagnostic.nuspec");

            NuGetPackage pkg = new NuGetPackage();

            pkg.Metadata = new Metadata();
            pkg.Metadata.Id = "id";
            pkg.Metadata.Version = "version";
            pkg.Metadata.Authors = "authors";

            pkg.Files = new NuGetFiles();
            pkg.Files.Items = new List<NuGetFile>();

            NuGetFile file1 = new NuGetFile()
            {
                Source = "source",
                Target = "target",
                Exclude = "exclude"
            };
            NuGetFile file2 = new NuGetFile()
            {
                Source = "source2",
                Target = "target2",
                Exclude = "exclude2"
            };

            pkg.Files.Items.Add(file1);
            pkg.Files.Items.Add(file2);


            // Act
            pkg.Save(filePath);
            this.TestContext.AddResultFile(filePath);
            NuGetPackage reloaded = NuGetPackage.Load(filePath);

            // Assert
            Assert.IsNotNull(reloaded);
            Assert.IsNotNull(reloaded.Metadata);
            Assert.IsNotNull(reloaded.Files);
            Assert.IsNotNull(reloaded.Files.Items);
            Assert.AreEqual(2, reloaded.Files.Items.Count);
        }

        [TestMethod]
        public void NuGet_ActualExample()
        {
            string testDir = TestUtils.CreateTestDirectory(this.TestContext);
            string filePath = TestUtils.CreateTextFile("diagnostic.txt", testDir,
@"<?xml version=""1.0""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd"">
  <metadata>
    <id>ExampleAnalyzer1</id>
    <version>1.0.0.0</version>
    <title>ExampleAnalyzer1</title>
    <authors>duncanp</authors>
    <owners>duncanp</owners>
    <licenseUrl>http://LICENSE_URL_HERE_OR_DELETE_THIS_LINE</licenseUrl>
    <projectUrl>http://PROJECT_URL_HERE_OR_DELETE_THIS_LINE</projectUrl>
    <iconUrl>http://ICON_URL_HERE_OR_DELETE_THIS_LINE</iconUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>ExampleAnalyzer1</description>
    <releaseNotes>Summary of changes made in this release of the package.</releaseNotes>
    <copyright>Copyright</copyright>
    <tags>ExampleAnalyzer1, analyzers</tags>
  </metadata>
  <!-- The convention for analyzers is to put language agnostic dlls in analyzers\dotnet and language specific analyzers in either analyzers\dotnet\cs or analyzers\dotnet\vb -->
  <files>
    <file src=""*.dll"" target=""analyzers\dotnet\cs"" exclude=""**\Microsoft.CodeAnalysis.*;**\System.Collections.Immutable.*;**\System.Reflection.Metadata.*;**\System.Composition.*"" />
    <file src=""tools\*.ps1"" target=""tools\"" />
  </files>
</package>");

            NuGetPackage pkg = NuGetPackage.Load(filePath);

            Assert.IsNotNull(pkg.Metadata);
            Assert.IsNotNull(pkg.Files);
            Assert.AreEqual(2, pkg.Files.Items.Count, "Unexpected number of files");
        }
    }
}
