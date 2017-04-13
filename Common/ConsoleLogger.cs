/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
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

using SonarQube.Common;
using System;

namespace SonarQube.Plugins.Common
{
    public class ConsoleLogger : ILogger
    {
        public const string DEBUG_PREFIX = "[DEBUG] ";
        public const string WARNING_PREFIX = "[WARNING] ";

        public const ConsoleColor DebugColor = ConsoleColor.DarkCyan;  
        public const ConsoleColor WarningColor = ConsoleColor.Yellow;  
        public const ConsoleColor ErrorColor = ConsoleColor.Red;

        #region ILogger interface

        public void LogDebug(string message, params object[] args)
        {
            using (new ConsoleColorScope(DebugColor))
            {
                Console.WriteLine(GetFormattedMessage(DEBUG_PREFIX, message, args));
            }
        }

        public void LogError(string message, params object[] args)
        {
            using (new ConsoleColorScope(ErrorColor))
            {
                Console.Error.WriteLine(GetFormattedMessage(string.Empty, message, args));
            }
        }

        public void LogInfo(string message, params object[] args)
        {
            Console.WriteLine(GetFormattedMessage(string.Empty, message, args));
        }

        public void LogWarning(string message, params object[] args)
        {
            using (new ConsoleColorScope(WarningColor))
            {
                Console.WriteLine(GetFormattedMessage(WARNING_PREFIX, message, args));
            }
        }

        #endregion

        private static string GetFormattedMessage(string prefix, string message, params object[] args)
        {
            string formattedMessage;
            if (args != null && args.Length > 0)
            {
                formattedMessage = string.Format(System.Globalization.CultureInfo.CurrentCulture, message, args);
            }
            else
            {
                 formattedMessage = message;
            }
            return prefix + formattedMessage;
        }
    }
}
