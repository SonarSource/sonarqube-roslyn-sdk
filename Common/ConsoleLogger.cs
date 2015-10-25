using System;

namespace Roslyn.SonarQube.Common
{
    public class ConsoleLogger : ILogger
    {
        public const string DEBUG_PREFIX = "[DEBUG] ";
        public const string WARNING_PREFIX = "[WARNING] ";

        #region ILogger interface

        public void LogDebug(string message, params object[] args)
        {
            Console.WriteLine(GetFormattedMessage(DEBUG_PREFIX, message, args));
        }

        public void LogError(string message, params object[] args)
        {
            Console.Error.WriteLine(GetFormattedMessage(DEBUG_PREFIX, message, args));
        }

        public void LogInfo(string message, params object[] args)
        {
            Console.WriteLine(GetFormattedMessage(string.Empty, message, args));
        }

        public void LogWarning(string message, params object[] args)
        {
            Console.WriteLine(GetFormattedMessage(WARNING_PREFIX, message, args));
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
