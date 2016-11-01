//-----------------------------------------------------------------------
// <copyright file="AssemblyResolver.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SonarQube.Plugins.Common
{

    /// <summary>
    /// Adds additional search directories for assembly resolution
    /// </summary>
    public sealed class AssemblyResolver : IDisposable
    {
        private const string DllExtension = ".dll";

        private readonly string[] rootSearchPaths;
        private readonly ILogger logger;

        public bool ResolverCalled { get; private set; } // for testing

        /// <summary>
        /// Create a new AssemblyResolver that will search in the given directories (recursively) for dependencies.
        /// </summary>
        /// <param name="rootSearchPaths">Additional search paths, assumed to be valid system directories</param>
        public AssemblyResolver(ILogger logger, params string[] rootSearchPaths)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (rootSearchPaths == null || rootSearchPaths.Length < 1)
            {
                throw new ArgumentException(Resources.Resolver_ConstructorNoPaths, "rootSearchPaths");
            }
            this.ResolverCalled = true;

            this.rootSearchPaths = rootSearchPaths;
            this.logger = logger;

            // This line required to resolve the Resources object before additional assembly resolution is added
            // Do not remove this line, otherwise CurrentDomain_AssemblyResolve will throw a StackOverflowException
            this.logger.LogDebug(Resources.Resolver_Initialize);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // This line causes a StackOverflowException unless Resources has already been called upon previously
            this.logger.LogDebug(Resources.Resolver_ResolvingAssembly, args.Name, args?.RequestingAssembly?.FullName ?? Resources.Resolver_UnspecifiedRequestingAssembly);
            Assembly asm;

            // The supplied assembly name could be a file name or an assembly full name. Work out which it is
            bool isFileName = Utilities.IsAssemblyLibraryFileName(args.Name);

            // Now work out the file name we are looking for
            string fileName = GetAssemblyFileName(args.Name);


            foreach (string rootSearchPath in rootSearchPaths)
            {
                foreach (string file in Directory.GetFiles(rootSearchPath, fileName, SearchOption.AllDirectories))
                {
                    asm = Assembly.LoadFile(file);

                    if (
                        // If the input was e.g foo.dll then compare against the file name...
                        (isFileName && string.Equals(Path.GetFileName(asm.Location), fileName,  StringComparison.OrdinalIgnoreCase))
                        ||
                        // ... otherwise compare against the full name
                        (!isFileName && string.Equals(args.Name, asm.FullName, StringComparison.OrdinalIgnoreCase))
                        )
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
        /// Turns the input assembly ref argument into an files name.
        /// The input might be a file name (e.g. foo.dll) or a full assembly name
        /// (e.g. SimpleAssemblyByFullName, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null)
        /// </summary>
        private static string GetAssemblyFileName(string input)
        {
            Debug.Assert(input != null);
            Debug.Assert(input.Length > 0);

            if (Utilities.IsAssemblyLibraryFileName(input))
            {
                return input;
            }

            AssemblyName assemblyName = new AssemblyName(input);
            return assemblyName.Name + DllExtension;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.logger.LogDebug(Resources.Resolver_Dispose);
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
