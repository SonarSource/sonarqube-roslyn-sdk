//-----------------------------------------------------------------------
// <copyright file="NuGetLoggerAdapter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using NuGet;
using System;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Adapter between the NuGet logger interface and the SonarQube logger interface
    /// </summary>
    internal class NuGetLoggerAdapter : NuGet.ILogger
    {
        private readonly Common.ILogger logger;

        public NuGetLoggerAdapter(Common.ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            this.logger = logger;
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case MessageLevel.Debug:
                    this.logger.LogDebug(message, args);
                    break;
                case MessageLevel.Error:
                    this.logger.LogError(message, args);
                    break;
                case MessageLevel.Warning:
                    this.logger.LogWarning(message, args);
                    break;
                default:
                    this.logger.LogInfo(message, args);
                    break;
            }
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            this.logger.LogDebug(UIResources.NG_FileConflictOccurred, message);
            return FileConflictResolution.Ignore;
        }
    }
}
