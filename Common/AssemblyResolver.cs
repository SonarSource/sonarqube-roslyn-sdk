using Roslyn.SonarQube.Common;
using System;
using System.IO;
using System.Reflection;

namespace Roslyn.SonarQube.AnalyzerPlugins
{
    public sealed class AssemblyResolver : IDisposable
    {
        private readonly string[] rootSearchPaths;
        private readonly ILogger logger;

        public AssemblyResolver(string rootSearchPath, ILogger logger) : this(new string[1] { rootSearchPath }, logger) { }

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
            this.logger.LogDebug(Resources.Resolver_Initialize);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) // set to public for test purposes
        {
            // This line fails unless Resources has already been resolved previously
            this.logger.LogDebug(Resources.Resolver_ResolvingAssembly, args.Name, args.RequestingAssembly.FullName);
            Assembly asm = null;

            // TODO
            string[] parts = args.Name.Split(new char[] { ' ' });
            string fileName = parts[0].Substring(0, parts[0].Length - 1) + ".dll";

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
