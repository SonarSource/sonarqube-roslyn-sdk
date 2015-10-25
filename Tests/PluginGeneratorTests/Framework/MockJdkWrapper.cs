using Roslyn.SonarQube.Common;
using Roslyn.SonarQube.PluginGenerator;
using System;
using System.Collections.Generic;

namespace Roslyn.SonarQube.PluginGeneratorTests
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
