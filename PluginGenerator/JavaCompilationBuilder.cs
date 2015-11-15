using Roslyn.SonarQube.Common;
using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Roslyn.SonarQube.PluginGenerator
{
    public class JavaCompilationBuilder
    {
        private readonly IJdkWrapper jdkWrapper;

        private readonly IList<string> classPaths;
        private readonly IList<string> sources;

        public JavaCompilationBuilder(IJdkWrapper jdkWrapper)
        {
            if (jdkWrapper == null)
            {
                throw new ArgumentNullException("jdkWrapper");
            }
            this.jdkWrapper = jdkWrapper;
            this.classPaths = new List<string>();
            this.sources = new List<string>();
        }

        public JavaCompilationBuilder AddSources(string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                throw new ArgumentNullException("sources");
            }
            this.sources.Add(sourceFile);

            return this;
        }

        public JavaCompilationBuilder AddClassPath(string classPath)
        {
            if (string.IsNullOrWhiteSpace(classPath))
            {
                throw new ArgumentNullException("classPath");
            }
            this.classPaths.Add(classPath);

            return this;
        }
        
        public bool Compile(string sourcesDirectory, string outputDirectory, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(sourcesDirectory))
            {
                throw new ArgumentNullException("sourcesDirectory");
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentNullException("outputDirectory");
            }
            
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            IList<string> args = new List<string>();

            StringBuilder sb = new StringBuilder();
            sb.Append("-cp " );
            foreach (string classPath in this.classPaths)
            {
                sb.Append(GetQuotedArg(classPath));
                sb.Append(";");
            }
            args.Add(sb.ToString());
            
            args.Add(string.Format(CultureInfo.CurrentCulture, " -d {0}", GetQuotedArg(outputDirectory)));

            args.Add(string.Format(CultureInfo.CurrentCulture, " -s {0}", GetQuotedArg(sourcesDirectory)));

            foreach (string source in this.sources)
            {
                args.Add(string.Format(CultureInfo.CurrentCulture, " {0}", GetQuotedArg(source)));
            }

            bool success = this.jdkWrapper.CompileSources(args, logger);

            return success;
        }


        private static string GetQuotedArg(string argument)
        {
            string quotedArg = argument;

            // If an argument contains a quote then we assume it has been correctly quoted.
            if (quotedArg != null && !argument.Contains('"'))
            {
                quotedArg = "\"" + argument + "\"";
            }

            return quotedArg;
        }
        
    }
}
