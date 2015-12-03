//-----------------------------------------------------------------------
// <copyright file="IJdkWrapper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Roslyn.SonarQube.Common;
using System.Collections.Generic;

namespace Roslyn.SonarQube.PluginGenerator
{
    /// <summary>
    /// Encapsulates the interactions with the JDK components
    /// </summary>
    public interface IJdkWrapper
    {
        bool IsJdkInstalled();

        bool CompileJar(string jarContentDirectory, string manifestFilePath, string fullJarPath, ILogger logger);

        bool CompileSources(IEnumerable<string> args, ILogger logger);

    }
}
