using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleGeneratorTests
{
    internal static class TestUtils
    {
        public static string CreateTestDir(TestContext context)
        {
            string fullPath = Path.Combine(context.TestRunDirectory, context.TestName);

            Assert.IsFalse(Directory.Exists(fullPath), "Test setup error: not expecting the test directory to exist - {0}", fullPath);
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }
    }
}
