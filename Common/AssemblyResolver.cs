using Roslyn.SonarQube.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Roslyn.SonarQube.AnalyzerPlugins
{

    /// <summary>
    /// Adds additional search directories for assembly resolution
    /// </summary>
    public sealed class AssemblyResolver : IDisposable
    {
        private readonly string[] rootSearchPaths;
        private readonly ILogger logger;

        /// <summary>
        /// Constructor specifying a single additional search directory.
        /// </summary>
        /// <param name="rootSearchPath">The search path</param>
        public AssemblyResolver(string rootSearchPath, ILogger logger) : this(new string[1] { rootSearchPath }, logger) { }

        /// <summary>
        /// Constructor specifying multiple additional search directories.
        /// </summary>
        /// <param name="rootSearchPaths">Additional search paths</param>
        public AssemblyResolver(string[] rootSearchPaths, ILogger logger)
        {
            foreach (string rootSearchPath in rootSearchPaths)
            {
                if (string.IsNullOrWhiteSpace(rootSearchPath))
                {
                    throw new ArgumentNullException("rootSearchPath");
                }
                if (logger == null)
                {
                    throw new ArgumentNullException("logger");
                }
            }
            if (rootSearchPaths.Length < 1)
            {
                throw new ArgumentNullException("rootSearchPaths");
            }

            this.rootSearchPaths = rootSearchPaths;            
            this.logger = logger;

            // This line required to resolve the Resources object before additional assembly resolution is added
            // Do not remove this line, otherwise CurrentDomain_AssemblyResolve will throw a StackOverflowException
            this.logger.LogDebug(Resources.Resolver_Initialize);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) // set to public for test purposes
        {
            // This line causes a StackOverflowException unless Resources has already been called upon previously
            this.logger.LogDebug(Resources.Resolver_ResolvingAssembly, args.Name, args.RequestingAssembly.FullName);
            Assembly asm = null;

            string fileName = CreateFileNameFromAssemblyName(args.Name);

            foreach (string rootSearchPath in rootSearchPaths)
            {
                foreach (string file in Directory.GetFiles(rootSearchPath, fileName, SearchOption.AllDirectories))
                {
                    asm = Assembly.LoadFile(file);

                    if (string.Equals(args.Name, asm.FullName))
                    {
                        this.logger.LogDebug(Resources.Resolver_AssemblyLocated, file);
                        return asm;
                    }
                    else
                    {
                        this.logger.LogDebug(Resources.Resolver_RejectedAssembly, asm.FullName);
                    }
                }
            }
            this.logger.LogDebug(Resources.Resolver_FailedToResolveAssembly);
            return null;
        }

        /// <summary>
        /// Attempts to create the name of the file associated with a given assembly name.
        /// </summary>
        public static string CreateFileNameFromAssemblyName(string input) // Public for testing purposes
        {
            Debug.Assert(input != null);
            Debug.Assert(input.Length > 0);
            Debug.Assert(!input.EndsWith(".dll"));

            // TODO ???
            string[] parts = input.Split(new char[] { ' ' });

            return parts[0].Substring(0, parts[0].Length - 1) + ".dll";
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
