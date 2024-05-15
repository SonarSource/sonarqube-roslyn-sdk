/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2022 SonarSource SA
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SonarQube.Plugins.Common;

namespace SonarQube.Common
{
    /// <summary>
    /// Process and validates the command line arguments and reports any errors
    /// </summary>
    /// <remarks>The command line parsing makes a number of simplifying assumptions:
    /// * order is unimportant
    /// * all arguments have a recognizable prefix e.g. /key=
    /// * the first matching prefix will be used (so if descriptors have overlapping prefixes they need
    ///   to be supplied to the parser in the correct order on construction)
    /// * the command line arguments are those supplied in Main(args) i.e. they have been converted
    ///   from a string to an array by the runtime. This means that quoted arguments will already have
    ///   been partially processed so a command line of:
    ///        myApp.exe "quoted arg" /k="ab cd" ""
    ///   will be supplied as three args, [quoted arg] , [/k=ab cd] and String.Empty</remarks>
    public class CommandLineParser
    {
        /// <summary>
        /// List of definitions of valid arguments
        /// </summary>
        private readonly IEnumerable<ArgumentDescriptor> descriptors;

        private readonly bool allowUnrecognized;

        /// <summary>
        /// Constructs a command line parser
        /// </summary>
        /// <param name="descriptors">List of descriptors that specify the valid argument types</param>
        /// <param name="allowUnrecognized">True if unrecognized arguments should be ignored</param>
        public CommandLineParser(IEnumerable<ArgumentDescriptor> descriptors, bool allowUnrecognized)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            if (descriptors.Select(d => d.Id).Distinct(ArgumentDescriptor.IdComparer).Count() != descriptors.Count())
            {
                throw new ArgumentException(Resources.ERROR_Parser_UniqueDescriptorIds, nameof(descriptors));
            }

            this.descriptors = descriptors;
            this.allowUnrecognized = allowUnrecognized;
        }

        /// <summary>
        /// Parses the supplied arguments. Logs errors for unrecognized, duplicate or missing arguments.
        /// </summary>
        /// <param name="argumentInstances">A list of argument instances that have been recognized</param>
        public bool ParseArguments(string[] commandLineArgs, ILogger logger, out IEnumerable<ArgumentInstance> argumentInstances)
        {
            if (commandLineArgs == null)
            {
                throw new ArgumentNullException(nameof(commandLineArgs));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            bool parsedOk = true;

            // List of values that have been recognized
            IList<ArgumentInstance> recognized = new List<ArgumentInstance>();

            foreach (string arg in commandLineArgs)
            {
                if (TryGetMatchingDescriptor(arg, out ArgumentDescriptor descriptor, out string prefix))
                {
                    string newId = descriptor.Id;

                    if (!descriptor.AllowMultiple && IdExists(newId, recognized))
                    {
                        ArgumentInstance.TryGetArgumentValue(newId, recognized, out string existingValue);
                        logger.LogError(Resources.ERROR_CmdLine_DuplicateArg, arg, existingValue);
                        parsedOk = false;
                    }
                    else
                    {
                        // Store the argument
                        string argValue = arg.Substring(prefix.Length);
                        recognized.Add(new ArgumentInstance(descriptor, argValue));
                    }
                }
                else
                {
                    if (!allowUnrecognized)
                    {
                        logger.LogError(Resources.ERROR_CmdLine_UnrecognizedArg, arg);
                        parsedOk = false;
                    }

                    Debug.WriteLineIf(allowUnrecognized, "Ignoring unrecognized argument: " + arg);
                }
            }

            // We'll check for missing arguments this even if the parsing failed so we output as much detail
            // as possible about the failures.
            parsedOk &= CheckRequiredArgumentsSupplied(recognized, logger);

            argumentInstances = parsedOk ? recognized : Enumerable.Empty<ArgumentInstance>();

            return parsedOk;
        }

        /// <summary>
        /// Attempts to find a descriptor for the current argument
        /// </summary>
        /// <param name="argument">The argument passed on the command line</param>
        /// <param name="descriptor">The descriptor that matches the argument</param>
        /// <param name="prefix">The specific prefix that was matched</param>
        private bool TryGetMatchingDescriptor(string argument, out ArgumentDescriptor descriptor, out string prefix)
        {
            descriptor = null;
            prefix = null;

            foreach (ArgumentDescriptor item in descriptors)
            {
                string match = TryGetMatchingPrefix(item, argument);
                if (match != null)
                {
                    descriptor = item;
                    prefix = match;
                    return true;
                }
            }
            return false;
        }

        private static string TryGetMatchingPrefix(ArgumentDescriptor descriptor, string argument)
        {
            Debug.Assert(descriptor.Prefixes.Count(p => argument.StartsWith(p, ArgumentDescriptor.IdComparison)) < 2,
                "Not expecting the argument to match multiple prefixes");

            return descriptor.IsVerb
                // Verbs match the whole argument
                ? descriptor.Prefixes.FirstOrDefault(p => ArgumentDescriptor.IdComparer.Equals(p, argument))
                // Prefixes only match the start
                : descriptor.Prefixes.FirstOrDefault(p => argument.StartsWith(p, ArgumentDescriptor.IdComparison));
        }

        private static bool IdExists(string id, IEnumerable<ArgumentInstance> arguments)
        {
            return ArgumentInstance.TryGetArgument(id, arguments, out ArgumentInstance existing);
        }

        /// <summary>
        /// Checks whether any required arguments are missing and logs error messages for them.
        /// </summary>
        private bool CheckRequiredArgumentsSupplied(IEnumerable<ArgumentInstance> arguments, ILogger logger)
        {
            foreach (ArgumentDescriptor desc in descriptors.Where(d => d.Required))
            {
                ArgumentInstance.TryGetArgument(desc.Id, arguments, out ArgumentInstance argument);

                bool exists = argument != null && !string.IsNullOrWhiteSpace(argument.Value);
                if (!exists)
                {
                    logger.LogError(Resources.ERROR_CmdLine_MissingRequiredArgument, desc.Prefixes[0]);
                    ShowHelpMessage(logger);
                    return false;
                }
            }
            return true;
        }

        private void ShowHelpMessage(ILogger logger)
        {
            logger.LogInfo(Resources.CmdLine_Help_RequiredArguments);
            foreach (ArgumentDescriptor descriptor in descriptors.Where(x => x.Required).OrderBy(x => x.Prefixes[0]))
            {
                DisplayArgumentHelp(descriptor);
            }
            logger.LogInfo(string.Empty);
            logger.LogInfo(Resources.CmdLine_Help_OptionalArguments);
            foreach (ArgumentDescriptor descriptor in descriptors.Where(x => !x.Required).OrderBy(x => x.Prefixes[0]))
            {
                DisplayArgumentHelp(descriptor);
            }

            void DisplayArgumentHelp(ArgumentDescriptor argument) =>
                logger.LogInfo(Resources.CmdLine_Help_Argument, string.Join(", ", argument.Prefixes), argument.Description);
        }
    }
}