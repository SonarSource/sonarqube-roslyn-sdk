using System;
using System.Reflection;

namespace Roslyn.SonarQube.Common
{
    public static class Utilities
    {
        public static void LogAssemblyVersion(Assembly assembly, string description, ILogger logger)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            logger.LogInfo("{0} {1}", description, assembly.GetName().Version);
        }
    }
}
