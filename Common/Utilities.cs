//-----------------------------------------------------------------------
// <copyright file="Utilities.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Reflection;

namespace SonarQube.Plugins.Common
{
    public static class Utilities
    {
        public static void LogAssemblyVersion(Assembly assembly, string description, ILogger logger)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            logger.LogInfo("{0} {1}", description, assembly.GetName().Version);
        }
    }
}
