//-----------------------------------------------------------------------
// <copyright file="RulesPluginBuilderTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System;
using System.IO;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class RulesPluginBuilderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void RulesPluginBuilder_CreateSimplePlugin_Succeeds()
        {
            string inputDir = TestUtils.CreateTestDirectory(this.TestContext, "input");
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, "output");
            string fullJarFilePath = Path.Combine(outputDir, "myPlugin.jar");

            string language = "xx";
            string rulesXmlFilePath = TestUtils.CreateTextFile("rules.xml", inputDir, "<xml Rules />");
            string sqaleXmlFilePath = TestUtils.CreateTextFile("sqale.xml", inputDir, "<xml sqale />");

            PluginManifest defn = new PluginManifest()
            {
                Key = "MyPlugin",
                Name = "My Plugin",
                Description = "Generated plugin",
                Version = "0.1-SNAPSHOT",
                Organization = "ACME Software Ltd",
                License = "Commercial",
                Developers = typeof(RulesPluginBuilder).FullName
            };

            MockJdkWrapper mockJdkWrapper = new MockJdkWrapper();

            RulesPluginBuilder builder = new RulesPluginBuilder(mockJdkWrapper, new MockMavenArtifactHandler(), new TestLogger());

            builder.SetLanguage(language)
                .SetRepositoryKey("repo.key")
                .SetRulesFilePath(rulesXmlFilePath)
                .SetSqaleFilePath(sqaleXmlFilePath)
                .SetJarFilePath(fullJarFilePath)
                .SetProperties(defn);

            builder.Build(); // should not fail
            mockJdkWrapper.AssertJarBuilt();
        }

        [TestMethod]
        public void RulesPluginBuilder_LanguageSetToNull_Fails()
        {
            // Arrange
            RulesPluginBuilder builder = new RulesPluginBuilder(new TestLogger());

            // Act and assert
            AssertException.Expect<ArgumentNullException>(() => builder.SetLanguage(null));
            AssertException.Expect<ArgumentNullException>(() => builder.SetLanguage(""));
        }

        [TestMethod]
        public void RulesPluginBuilder_RulesFileSetToNull_Fails()
        {
            // Arrange
            RulesPluginBuilder builder = new RulesPluginBuilder(new TestLogger());

            // Act and assert
            AssertException.Expect<ArgumentNullException>(() => builder.SetRulesFilePath(null));
            AssertException.Expect<ArgumentNullException>(() => builder.SetRulesFilePath(""));
        }

        [TestMethod]
        public void RulesPluginBuilder_SqaleFileSetToNull_Fails()
        {
            // Arrange
            RulesPluginBuilder builder = new RulesPluginBuilder(new TestLogger());

            // Act and assert
            AssertException.Expect<ArgumentNullException>(() => builder.SetSqaleFilePath(null));
            AssertException.Expect<ArgumentNullException>(() => builder.SetSqaleFilePath(""));
        }

        [TestMethod]
        public void RulesPluginBuilder_RulesFileValidation()
        {
            // Arrange
            MockJdkWrapper mockJdkWrapper = new MockJdkWrapper();
            RulesPluginBuilder builder = new RulesPluginBuilder(mockJdkWrapper, new MockMavenArtifactHandler(), new TestLogger());
            SetValidCoreProperties(builder);
            builder.SetLanguage("aLanguage");
            builder.SetRepositoryKey("repo.key");

            // 1. Rules file not specified -> error
            AssertException.Expect<InvalidOperationException>(() => builder.Build());

            // 2. Non-existent rules file specified -> error
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext);
            string rulesFile = Path.Combine(testDir, "missingFile.txt");
            mockJdkWrapper.AssertCodeNotCompiled();

            builder.SetRulesFilePath(rulesFile);
            FileNotFoundException ex = AssertException.Expect<FileNotFoundException>(() => builder.Build());
            Assert.AreEqual(ex.FileName, rulesFile);
            mockJdkWrapper.AssertCodeNotCompiled();

            // 3. Rules file exists -> succeeds
            AddValidDummyRulesFiles(builder);
            builder.Build(); // should succeed
            mockJdkWrapper.AssertJarBuilt();
        }

        [TestMethod]
        public void RulesPluginBuilder_SqaleFileValidation()
        {
            // Arrange
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext);

            MockJdkWrapper mockJdkWrapper = new MockJdkWrapper();
            RulesPluginBuilder builder = new RulesPluginBuilder(mockJdkWrapper, new MockMavenArtifactHandler(), new TestLogger());
            SetValidCoreProperties(builder);
            builder.SetLanguage("aLanguage");
            builder.SetRepositoryKey("repo");
            AddValidDummyRulesFiles(builder);

            // 1. Sqale file not specified -> ok
            builder.Build();

            // 2. Non-existent Sqale file specified -> error
            mockJdkWrapper.ClearCalledMethodList();

            string sqaleFile = Path.Combine(testDir, "missingFile.txt");
            builder.SetSqaleFilePath(sqaleFile);
            FileNotFoundException ex = AssertException.Expect<FileNotFoundException>(() => builder.Build());
            Assert.AreEqual(ex.FileName, sqaleFile);
            mockJdkWrapper.AssertCodeNotCompiled();

            // 3. Sqale file exists -> succeeds
            sqaleFile = TestUtils.CreateTextFile("sqale.txt", testDir, "dummy sqale file");
            builder.SetSqaleFilePath(sqaleFile);
            builder.Build(); // should succeed
            mockJdkWrapper.AssertJarBuilt();
        }

        [TestMethod]
        public void RulesPluginBuilder_LanguageIsRequired()
        {
            // Arrange
            RulesPluginBuilder builder = new RulesPluginBuilder(new TestLogger());
            SetValidCoreProperties(builder);
            AddValidDummyRulesFiles(builder);

            // Act and assert
            AssertException.Expect<InvalidOperationException>(() => builder.Build());
        }

        #region Private methods

        private void SetValidCoreProperties(RulesPluginBuilder builder)
        {
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext);

            builder.SetPluginKey("dummykey")
                .SetPluginName("dummy name")
                .SetJarFilePath(Path.Combine(testDir, "dummy.jar.txt"));
        }

        private void AddValidDummyRulesFiles(RulesPluginBuilder builder)
        {
            string testDir = TestUtils.EnsureTestDirectoryExists(this.TestContext);
            string rulesFile = TestUtils.CreateTextFile("rules.txt", testDir, "dummy rules file");

            builder.SetRulesFilePath(rulesFile);
        }

        #endregion
    }
}
