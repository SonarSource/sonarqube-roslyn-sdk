using Roslyn.SonarQube.Common;
using System;
using System.IO;
using System.Reflection;

namespace Roslyn.SonarQube.AnalyzerPlugins
{
    internal sealed class AssemblyResolver : IDisposable
    {
        private readonly string rootSearchPath;
        private readonly ILogger logger;

        public AssemblyResolver(string rootSearchPath, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(rootSearchPath))
            {
                throw new ArgumentNullException("rootSearchPath");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.rootSearchPath = rootSearchPath;
            this.logger = logger;

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            this.logger.LogDebug(UIResources.Resolver_ResolvingAssembly, args.Name, args.RequestingAssembly.FullName);
            Assembly asm = null;

            // TODO
            string[] parts = args.Name.Split(new char[] { ' ' });
            string fileName = parts[0].Substring(0, parts[0].Length - 1) + ".dll";

            foreach (string file in Directory.GetFiles(this.rootSearchPath, fileName, SearchOption.AllDirectories))
            {
                asm = Assembly.LoadFile(file);

                if (string.Equals(args.Name, asm.FullName))
                {
                    this.logger.LogDebug(UIResources.Resolver_AssemblyLocated, file);
                    return asm;
                }
                else
                {
                    this.logger.LogDebug(UIResources.Resolver_RejectedAssembly, asm.FullName);
                }
            }
            this.logger.LogDebug(UIResources.Resolver_FailedToResolveAssembly);
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
