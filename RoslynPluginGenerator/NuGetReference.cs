using System;

namespace Roslyn.SonarQube.AnalyzerPlugins.CommandLine
{
    public class NuGetReference
    {
        private readonly string packageId;
        private readonly NuGet.SemanticVersion version;

        public NuGetReference(string packageId, NuGet.SemanticVersion version)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException("packageId");
            }
            this.packageId = packageId;
            this.version = version;
        }

        public string PackageId { get { return this.packageId; } }
        public NuGet.SemanticVersion Version { get { return this.version; } }
    }
}
