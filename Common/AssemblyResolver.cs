//-----------------------------------------------------------------------
// <copyright file="AssemblyResolver.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
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
        /// Create a new AssemblyResolver that will search in the given directories (recursively) for dependencies.
        /// </summary>
        /// <param name="rootSearchPaths">Additional search paths, assumed to be valid system directories</param>
        public AssemblyResolver(ILogger logger, params string[] rootSearchPaths)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            } else if (rootSearchPaths == null || rootSearchPaths.Length < 1)
            {
                throw new ArgumentException(Resources.Resolver_ConstructorNoPaths);
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

                    if (string.Equals(args.Name, asm.FullName, StringComparison.CurrentCultureIgnoreCase))
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

            if (input.EndsWith(".dll"))
            {
                return input;
            }

            string result = input;
            if (input.Contains(" "))
            {
                // If the assembly name has multiple words (seperated by spaces), use only the first word
                // (e.g. "foo bar" -> "foo")
                string[] parts = input.Split(new char[] { ' ' });
                result = parts[0].Substring(0, parts[0].Length - 1);
            }

            return  result + ".dll";
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
