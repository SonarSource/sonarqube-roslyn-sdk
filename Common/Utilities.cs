//-----------------------------------------------------------------------
// <copyright file="Utilities.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.IO;
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

        public static string CreateTempDirectory(string dirName)
        {
            string newPath = Path.GetTempPath();
            newPath = Path.Combine(newPath, ".sqsdk", dirName);
            Directory.CreateDirectory(newPath);
            return newPath;
        }

        public static string CreateSubDirectory(string parent, string child)
        {
            if (string.IsNullOrWhiteSpace(parent))
            {
                throw new ArgumentNullException("parent");
            }
            if (string.IsNullOrWhiteSpace(child))
            {
                throw new ArgumentNullException("child");
            }

            string newDir = Path.Combine(parent, child);
            Directory.CreateDirectory(newDir);
            return newDir;
        }

    }
}
