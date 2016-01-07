//-----------------------------------------------------------------------
// <copyright file="MockJdkWrapper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Plugins.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    internal class MockJdkWrapper : IJdkWrapper
    {
        private const string CompileSourcesMethod = "CompileSources";
        private const string CompileJarMethod = "CompileJar";

        private readonly List<string> calledMethods = new List<string>();

        public MockJdkWrapper()
        {
            this.calledMethods = new List<string>();

            // Set values so the calls succed by default
            this.IsJdkInstalledReturnValue = true;
            this.CompileSourcesReturnValue = true;
            this.CompileJarReturnValue = true;
        }

        #region Test helpers

        public bool IsJdkInstalledReturnValue { get; set; }

        public bool CompileSourcesReturnValue { get; set; }

        public bool CompileJarReturnValue { get; set; }

        public void AssertJarBuilt()
        {
            CollectionAssert.Contains(this.calledMethods, CompileSourcesMethod, "Source was not compiled");
            CollectionAssert.Contains(this.calledMethods, CompileJarMethod, "Jar was not built");
        }

        public void AssertCodeNotCompiled()
        {
            CollectionAssert.DoesNotContain(this.calledMethods, CompileSourcesMethod, "Not expecting source to have been called");
            CollectionAssert.DoesNotContain(this.calledMethods, CompileJarMethod, "Not expecting jar to have been built");
        }

        public void ClearCalledMethodList()
        {
            this.calledMethods.Clear();
        }

        #endregion

        #region IJdkWrapper methods

        bool IJdkWrapper.CompileJar(string jarContentDirectory, string manifestFilePath, string fullJarPath, ILogger logger)
        {
            this.RecordMethodCall();

            Assert.IsNotNull(jarContentDirectory);
            Assert.IsTrue(Directory.Exists(jarContentDirectory), "Jar content directory must exist");

            Assert.IsNotNull(manifestFilePath);
            Assert.IsTrue(File.Exists(manifestFilePath), "Manifest file must exist");

            Assert.IsNotNull(fullJarPath);
            Assert.IsNotNull(logger);

            return this.CompileJarReturnValue;
        }

        bool IJdkWrapper.IsJdkInstalled()
        {
            this.RecordMethodCall();
            return this.IsJdkInstalledReturnValue;
        }

        bool IJdkWrapper.CompileSources(IEnumerable<string> args, ILogger logger)
        {
            this.RecordMethodCall();

            Assert.IsNotNull(args);
            Assert.IsTrue(args.Any());
            Assert.IsNotNull(logger);

            return this.CompileSourcesReturnValue;
        }

        #endregion
        
        private void RecordMethodCall([CallerMemberNameAttribute] string callerName = null)
        {
            this.calledMethods.Add(callerName);
        }
    }
}