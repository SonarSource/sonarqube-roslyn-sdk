using SonarQube.Common;
using System;

namespace Roslyn.SonarQube.Common
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
