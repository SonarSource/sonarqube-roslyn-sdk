//-----------------------------------------------------------------------
// <copyright file="NuGetReference.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using System;

namespace SonarQube.Plugins.Roslyn.CommandLine
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
