using System;

namespace Roslyn.SonarQube.AnalyzerPlugins.CommandLine
{
    public class ProcessedArgs
    {
        private readonly NuGetReference analyzerRef;

        public ProcessedArgs(NuGetReference analyzerRef)
        {
            if (analyzerRef == null)
            {
                throw new ArgumentNullException("analyzerRef");
            }
            this.analyzerRef = analyzerRef;
        }

        public NuGetReference AnalyzerRef { get { return this.analyzerRef; } }

    }
}
