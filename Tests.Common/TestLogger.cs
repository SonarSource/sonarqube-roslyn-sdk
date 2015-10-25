using PluginGenerator;
using Roslyn.SonarQube.Common;
using System;
using System.Collections.Generic;

namespace Tests.Common
{
    public class TestLogger : ILogger
    {
        public enum MessageType
        {
            Info,
            Debug,
            Warning,
            Error
        }

        private IList<Tuple<MessageType, string>> messages; 

        public TestLogger()
        {
            this.messages = new List<Tuple<MessageType, string>>();
        }

        #region ILogger interface methods

        public void LogDebug(string message, params object[] args)
        {
            this.RecordMessage(MessageType.Debug, message, args);
        }

        public void LogError(string message, params object[] args)
        {
            this.RecordMessage(MessageType.Error, message, args);
        }

        public void LogInfo(string message, params object[] args)
        {
            this.RecordMessage(MessageType.Info, message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            this.RecordMessage(MessageType.Warning, message, args);
        }

        #endregion

        private void RecordMessage(MessageType type, string message, params object[] args)
        {
            string formattedMessage = message;
            if (args != null && args.Length > 0)
            {
                formattedMessage = string.Format(System.Globalization.CultureInfo.CurrentCulture, message, args);
            }

            this.messages.Add(new Tuple<MessageType, string>(type, formattedMessage));
            Console.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0}: {1}", type, formattedMessage));
        }
    }
}
