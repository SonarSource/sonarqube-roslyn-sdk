using SonarQube.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginGenerator
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
        
        public bool Compile(string outputDirectory, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentNullException("outputDirectory");
            }
            
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            IList<string> args = new List<string>();

            foreach (string classPath in this.classPaths)
            {
                args.Add(string.Format(CultureInfo.CurrentCulture, "-cp {0} ", ProcessRunnerArguments.GetQuotedArg(classPath)));
            }

            foreach(string source in this.sources)
            {
                args.Add(string.Format(CultureInfo.CurrentCulture, "{0} ", ProcessRunnerArguments.GetQuotedArg(source)));
            }

            args.Add(string.Format(CultureInfo.CurrentCulture, "-d {0}", ProcessRunnerArguments.GetQuotedArg(outputDirectory)));

            bool success = this.jdkWrapper.CompileSources(args, logger);

            return success;
        }

    }
}
