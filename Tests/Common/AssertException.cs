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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarQube.Plugins.Test.Common
{
    public static class AssertException
    {
        /// <summary>
        /// Checks that the expected exception is thrown when the code is executed
        /// </summary>
        public static T Expect<T>(Action op) where T : Exception
        {
            T actual = null;
            try
            {
                op();
            }
            catch(Exception ex)
            {
                Assert.IsTrue(typeof(T).IsAssignableFrom(ex.GetType()),
                    "Thrown exception is not of the expected type. Expected: {0}, actual: {1}",
                    typeof(T).FullName,
                    ex.GetType().FullName);
                actual = (T)ex;
            }

            Assert.IsNotNull(actual, "Expecting an exception to be thrown");
            return actual;
        }
    }
}
