using System;

namespace Roslyn.SonarQube.AnalyzerPlugins.CommandLine
{
    public class ProcessedArgs
    {
        private readonly NuGetReference analyzerRef;
        private readonly string sqaleFilePath;

        public ProcessedArgs(NuGetReference analyzerRef, string sqaleFilePath)
        {
            if (analyzerRef == null)
            {
                throw new ArgumentNullException("analyzerRef");
            }
            this.analyzerRef = analyzerRef;
            this.sqaleFilePath = sqaleFilePath; // can be null
        }

        public NuGetReference AnalyzerRef { get { return this.analyzerRef; } }

        public string SqaleFilePath {  get { return this.sqaleFilePath; } }
    }
}
