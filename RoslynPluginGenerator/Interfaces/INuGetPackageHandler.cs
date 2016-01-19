//-----------------------------------------------------------------------
// <copyright file="INuGetPackageHandler.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using NuGet;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Enacapsulates the interactions with NuGet
    /// </summary>
    public interface INuGetPackageHandler
    {

        string localCacheRoot { get; }

        /// <summary>
        /// Attempts to fetch the specified package
        /// </summary>
        /// <param name="packageId">The package identifier</param>
        /// <param name="version">The version of the package to download. Can be null,
        /// in which case the most recent version will be downloaded (which could be 
        /// a pre-release version).</param>
        /// 
        /// <returns>A reference to the package, or null if the package could not be located</returns>
        IPackage FetchPackage(string packageId, SemanticVersion version);
    }
}
