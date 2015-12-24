//-----------------------------------------------------------------------
// <copyright file="PluginBuilderTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Test.Common;
using System.IO;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class PluginBuilderTests
    {
        public TestContext TestContext { get; set; }

        #region Tests

        [TestMethod]
        public void PluginBuilder_Simple()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            PluginBuilder builder = CreateValidBuilder(logger);

            // Act and assert
            BuildAndCheckSucceeds(builder, logger);
        }

        [TestMethod]
        public void PluginBuilder_PluginKeyIsRequired()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            PluginBuilder builder = CreateValidBuilder(logger);
            builder.SetProperty(WellKnownPluginProperties.Key, null);

            // Act and assert
            AssertException.Expect<System.InvalidOperationException>(() => builder.Build());
        }

        [TestMethod]
        public void PluginBuilder_PluginNameIsRequired()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            PluginBuilder builder = CreateValidBuilder(logger);
            builder.SetProperty(WellKnownPluginProperties.PluginName, null);

            // Act and assert
            AssertException.Expect<System.InvalidOperationException>(() => builder.Build());
        }

        [TestMethod]
        public void PluginBuilder_InvalidSource()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            PluginBuilder builder = CreateValidBuilder(logger);

            string invalidSource = this.CreateInputSourceFile("Program.java", "invalid java code");
            builder.AddSourceFile(invalidSource);

            // Act
            BuildAndCheckCompileFails(builder, logger);
        }

        [TestMethod]
        public void PluginBuilder_Extensions_Required()
        {
            string inputDir = TestUtils.CreateTestDirectory(this.TestContext, "input");
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, "output");

            string pluginFilePath = Path.Combine(outputDir, "plugin1.jar");
            string source1 = TestUtils.CreateTextFile("Program.java", inputDir,
@"package myorg.app1;
public final class Program {}
");
            TestLogger logger = new TestLogger();
            PluginBuilder builder = new PluginBuilder(logger);

            builder
                .AddSourceFile(source1)
                .SetJarFilePath(pluginFilePath)
                .SetPluginKey("dummy.key")
                .SetPluginName("dummy name");

            //Act and assert
            AssertException.Expect<System.InvalidOperationException>(() => builder.Build());
        }

        [TestMethod]
        public void PluginBuilder_Extensions_MultipleValid()
        {
            // Arrange
            TestLogger logger = new TestLogger();
            PluginBuilder builder = CreateValidBuilder(logger);

            string secondValidSource = this.CreateInputSourceFile("MyClass2.java",
@"package myPackage1;
public class MyClass2{}");
            builder.AddSourceFile(secondValidSource);
            builder.AddExtension("myPackage1.MyClass2.class");

            // Act and assert
            BuildAndCheckSucceeds(builder, logger);
        }

        [TestMethod]
        public void PluginBuilder_Extensions_PrivateExtension_Invalid()
        {
            // Checks compilation fails if one of the specified extensions
            // is not public.
            // In this case, the compilation should fail because the extension isn't
            // visible outside the package, so it can be exposed by the core "Plugin" class.

            // Arrange
            TestLogger logger = new TestLogger();
            PluginBuilder builder = CreateValidBuilder(logger);

            string secondValidSource = this.CreateInputSourceFile("MyPrivateClass.java",
@"package myPackage1;
private class MyPrivateClass{}");
            builder.AddSourceFile(secondValidSource);
            builder.AddExtension("myPackage1.MyClass2");

            // Act and assert
            BuildAndCheckCompileFails(builder, logger);
        }

        [TestMethod]
        public void PluginBuilder_Extensions_UnknownExtension_Invalid()
        {
            // Checks compilation fails if one of the specified extensions
            // cannot be found i.e. wrong class name

            // Arrange
            TestLogger logger = new TestLogger();
            PluginBuilder builder = CreateValidBuilder(logger);

            string secondValidSource = this.CreateInputSourceFile("AnotherValidClass.java",
@"package myPackage1;
public class AnotherValidClass{}");
            builder.AddSourceFile(secondValidSource);

            builder.AddExtension("myPackage1.unknownExtensionClass");

            // Act and assert
            BuildAndCheckCompileFails(builder, logger);
        }

        #endregion

        #region Private methods

        private PluginBuilder CreateValidBuilder(TestLogger logger)
        {
            string outputDir = TestUtils.CreateTestDirectory(this.TestContext, "output");

            string pluginFilePath = Path.Combine(outputDir, "plugin1.jar");

            string source1 = CreateInputSourceFile("MyExtensionClass",
@"package myorg.app1;
public final class MyExtensionClass
{
}
");
            PluginBuilder builder = new PluginBuilder(logger);

            builder
                .AddSourceFile(source1)
                .SetJarFilePath(pluginFilePath)
                .SetProperty("Property1", "prop 1 value")
                .AddExtension("myorg.app1.MyExtensionClass.class")
                .SetPluginKey("dummy.key")
                .SetPluginName("plugin name");

            return builder;
        }

        private string CreateInputSourceFile(string className, string content)
        {
            string inputDir = TestUtils.EnsureTestDirectoryExists(this.TestContext, "src");
            string fileName = className.EndsWith(".java", System.StringComparison.InvariantCultureIgnoreCase)
                ? className : className + ".java";
            return TestUtils.CreateTextFile(fileName, inputDir, content);
        }

        #endregion

        #region Checks

        private void BuildAndCheckSucceeds(PluginBuilder builder, TestLogger logger)
        {
            builder.Build();

            Assert.IsNotNull(builder.JarFilePath, "Expecting the jar file path to be set");
            TestUtils.AssertFileExists(builder.JarFilePath);
            this.TestContext.AddResultFile(builder.JarFilePath);

            logger.AssertErrorsLogged(0);
        }

        private void BuildAndCheckCompileFails(PluginBuilder builder, TestLogger logger)
        {
            AssertException.Expect<JavaCompilerException>(() => builder.Build());

            Assert.IsNotNull(builder.JarFilePath, "Expecting the jar file path to be set");
            TestUtils.AssertFileDoesNotExist(builder.JarFilePath);
            logger.AssertErrorsLogged();
        }
        #endregion
    }
}
