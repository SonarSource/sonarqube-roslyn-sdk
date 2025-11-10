/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource Sàrl
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using NuGet;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Adapter between the NuGet logger interface and the SonarQube logger interface
    /// </summary>
    public class NuGetLoggerAdapter : NuGet.ILogger
    {
        private readonly Common.ILogger logger;

        public const string LogMessagePrefix = "[NuGet] "; // does not need to be localised

        public NuGetLoggerAdapter(Common.ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            // Add a prefix to the message to make it easier to determine the source
            string prefixedMessage = LogMessagePrefix + message;
            switch (level)
            {
                case MessageLevel.Debug:
                    logger.LogDebug(prefixedMessage, args);
                    break;

                case MessageLevel.Error:
                    logger.LogError(prefixedMessage, args);
                    break;

                case MessageLevel.Warning:
                    logger.LogWarning(prefixedMessage, args);
                    break;

                default:
                    logger.LogInfo(prefixedMessage, args);
                    break;
            }
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            logger.LogDebug(UIResources.NG_FileConflictOccurred, message);
            return FileConflictResolution.Ignore;
        }
    }
}