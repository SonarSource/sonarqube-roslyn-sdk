using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tests.Common
{
    public static class AssertException
    {
        /// <summary>
        /// Checks that the expected exception is thrown when the code is executed
        /// </summary>
        public static Exception Expect<T>(Action op)
        {
            Exception actual = null;
            try
            {
                op();
            }
            catch(Exception ex)
            {
                Assert.IsTrue(typeof(T).IsAssignableFrom(ex.GetType()), "Thrown exception is not of the expected type");
                actual = ex;
            }

            Assert.IsNotNull(actual, "Expecting an exception to be throw");
            return actual;
        }
    }
}
