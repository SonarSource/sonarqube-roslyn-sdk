using Roslyn.SonarQube.Common;
using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PluginGenerator
{
    public class JdkWrapper : IJdkWrapper
    {
        public const string MANIFEST_FILE_NAME = "MANIFEST.MF";

        public const string JAVA_HOME_ENV_VAR = "JAVA_HOME";

        public const string JAVA_COMPILER_EXE = "javac.exe";
        public const string JAR_BUILDER_EXE = "jar.exe";

        #region IJdkWrapper methods

        public bool CompileJar(string jarContentDirectory, string manifestFilePath, string fullJarPath, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(fullJarPath))
            {
                throw new ArgumentNullException("fullJarPath");
            }
            if (string.IsNullOrWhiteSpace(jarContentDirectory))
            {
                throw new ArgumentNullException("jarLayoutDirectory");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            string jarExePath = TryFindJavaExe(JAR_BUILDER_EXE);
            Debug.Assert(!string.IsNullOrEmpty(jarExePath), "Failed to locate the jar exe, although the JDK appears to be installed");

            // jar cvfm test.jar MANIFEST.MF myorg\*.class* resources\*.*
            string[] cmdLineArgs = new string[]
                {
                "cvfm",
                ProcessRunnerArguments.GetQuotedArg(fullJarPath),
                ProcessRunnerArguments.GetQuotedArg(manifestFilePath),
                "." // directory - will be processed recursively
                };

            ProcessRunnerArguments runnerArgs = new ProcessRunnerArguments(jarExePath, logger)
            {
                CmdLineArgs = cmdLineArgs,
                WorkingDirectory = jarContentDirectory
            };

            ProcessRunner runner = new ProcessRunner();
            bool success = runner.Execute(runnerArgs);

            return success;
        }

        public bool IsJdkInstalled()
        {
            string jarExePath = TryFindJavaExe(JAVA_COMPILER_EXE);
            return jarExePath != null;
        }

        public bool CompileSources(IEnumerable<string> args, ILogger logger)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            string exePath = TryFindJavaExe(JAVA_COMPILER_EXE);
            Debug.Assert(!string.IsNullOrEmpty(exePath), "Failed to locate the Java compiler exe, although the JDK appears to be installed");

            ProcessRunnerArguments runnerArgs = new ProcessRunnerArguments(exePath, logger)
            {
                CmdLineArgs = args
            };

            ProcessRunner runner = new ProcessRunner();
            bool success = runner.Execute(runnerArgs);

            return success;
        }

        #endregion

        private static string TryFindJavaExe(string exeName)
        {
            string exePath = null;
            string javaHome = Environment.GetEnvironmentVariable(JAVA_HOME_ENV_VAR);

            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                exePath = Path.Combine(javaHome, "bin", exeName);
            }

            if (!File.Exists(exePath))
            {
                exePath = NativeMethods.FindOnPath(exeName);
                if (exePath != null && !File.Exists(exePath))
                {
                    exePath = null;
                }
            }
            return exePath;
        }
    }
}
