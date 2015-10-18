using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tests.Common
{
    public static class TestUtils
    {
        public static string CreateTestDirectory(TestContext testContext, params string[] subDirs)
        {
            List<string> parts = new List<string>();
            parts.Add(testContext.TestDeploymentDir);
            parts.Add(testContext.TestName);

            if (subDirs.Any())
            {
                parts.AddRange(subDirs);
            }

            string fullPath = Path.Combine(parts.ToArray());
            Assert.IsFalse(Directory.Exists(fullPath), "Test directory should not already exist: {0}", fullPath);
            Directory.CreateDirectory(fullPath);

            Console.WriteLine("Test setup: created directory: {0}", fullPath);

            return fullPath;
        }

        public static string AssertFileExists(string fileName, string outputDir)
        {
            string fullPath = Path.Combine(outputDir, fileName);
            Assert.IsTrue(File.Exists(fullPath), "Expected file does not exist: {0}", fullPath);

            return File.ReadAllText(fullPath);
        }

        public static string CreateTextFile(string fileName, string directory, string content = null)
        {
            string fullPath = Path.Combine(directory, fileName);
            File.WriteAllText(fullPath, content ?? string.Empty);
            return fullPath;
        }
    }
}
