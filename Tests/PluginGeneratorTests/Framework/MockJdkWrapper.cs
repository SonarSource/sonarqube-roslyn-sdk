//-----------------------------------------------------------------------
// <copyright file="MockJdkWrapper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarQube.Plugins.Common;
using SonarQube.Plugins;
using System;
using System.Collections.Generic;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    internal class MockJdkWrapper : IJdkWrapper
    {
        #region IJdkWrapper methods

        public bool CompileJar(string jarContentDirectory, string manifestFilePath, string fullJarPath, ILogger logger)
        {
            throw new NotImplementedException();
        }

        public bool IsJdkInstalled()
        {
            throw new NotImplementedException();
        }

        public bool CompileSources(IEnumerable<string> args, ILogger logger)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
