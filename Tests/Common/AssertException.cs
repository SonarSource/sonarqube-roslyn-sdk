//-----------------------------------------------------------------------
// <copyright file="AssertException.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
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
