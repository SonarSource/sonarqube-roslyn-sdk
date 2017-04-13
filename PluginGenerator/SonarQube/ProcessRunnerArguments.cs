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

using SonarQube.Plugins.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace SonarQube.Common
{
    /// <summary>
    /// Data class containing parameters required to execute a new process
    /// </summary>
    public class ProcessRunnerArguments
    {
        private readonly string exeName;
        private readonly ILogger logger;

        /// <summary>
        /// Strings that are used to indicate arguments that contain
        /// sensitive data that should not be logged
        /// </summary>
        public static readonly string[] SensitivePropertyKeys = new string[] { };


        public ProcessRunnerArguments(string exeName, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(exeName))
            {
                throw new ArgumentNullException("exeName");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            this.exeName = exeName;
            this.logger = logger;

            this.TimeoutInMilliseconds = Timeout.Infinite;
        }

        #region Public properties

        public string ExeName { get { return this.exeName; } }

        /// <summary>
        /// Non-sensitive command line arguments (i.e. ones that can safely be logged). Optional.
        /// </summary>
        public IEnumerable<string> CmdLineArgs { get; set; }

        public string WorkingDirectory { get; set; }

        public int TimeoutInMilliseconds { get; set; }

        /// <summary>
        /// Additional environments variables that should be set/overridden for the process. Can be null.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        public ILogger Logger { get { return this.logger; } }

        public string GetQuotedCommandLineArgs()
        {
            if (this.CmdLineArgs == null) { return null; }

            return string.Join(" ", this.CmdLineArgs.Select(a => GetQuotedArg(a)));
        }

        /// <summary>
        /// Returns the string that should be used when logging command line arguments
        /// (sensitive data will have been removed)
        /// </summary>
        public string GetCommandLineArgsLogText()
        {
            if (this.CmdLineArgs == null) { return null; }

            bool hasSensitiveData = false;

            StringBuilder sb = new StringBuilder();

            foreach (string arg in this.CmdLineArgs)
            {
                if (ContainsSensitiveData(arg))
                {
                    hasSensitiveData = true;
                }
                else
                {
                    sb.Append(arg);
                    sb.Append(" ");
                }
            }

            if (hasSensitiveData)
            {
                sb.Append(Resources.MSG_CmdLine_SensitiveCmdLineArgsAlternativeText);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the text contains sensitive data that
        /// should not be logged/written to file
        /// </summary>
        public static bool ContainsSensitiveData(string text)
        {
            Debug.Assert(SensitivePropertyKeys != null, "SensitiveDataMarkers array should not be null");

            if (text == null)
            {
                return false;
            }

            return SensitivePropertyKeys.Any(marker => text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) > -1);
        }

        public static string GetQuotedArg(string arg)
        {
            Debug.Assert(arg != null, "Not expecting an argument to be null");

            string quotedArg = arg;

            // If an argument contains a quote then we assume it has been correctly quoted.
            // Otherwise, quote strings that contain spaces.
            if (arg.Contains(' ') && !arg.Contains('"'))
            {
                quotedArg = "\"" + arg + "\"";
            }

            return quotedArg;
        }

        #endregion

    }
}
