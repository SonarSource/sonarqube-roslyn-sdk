using PluginGenerator;
using System;

namespace PluginGeneratorTests
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

        #endregion
    }
}
