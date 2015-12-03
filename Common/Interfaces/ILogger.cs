//-----------------------------------------------------------------------
// <copyright file="ILogger.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
namespace Roslyn.SonarQube.Common
{
    public interface ILogger
    {
        void LogInfo(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
    }
}
