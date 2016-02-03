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

        private const string LogMessagePrefix = "[NuGet] "; // does not need to be localised

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
            // Add a prefix to the message to make it easier to determine the source
            string prefixedMessage = LogMessagePrefix + message;
            switch (level)
            {
                case MessageLevel.Debug:
                    this.logger.LogDebug(prefixedMessage, args);
                    break;
                case MessageLevel.Error:
                    this.logger.LogError(prefixedMessage, args);
                    break;
                case MessageLevel.Warning:
                    this.logger.LogWarning(prefixedMessage, args);
                    break;
                default:
                    this.logger.LogInfo(prefixedMessage, args);
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
