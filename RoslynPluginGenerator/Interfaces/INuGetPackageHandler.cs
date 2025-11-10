/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2025 SonarSource Sàrl
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Collections.Generic;
using NuGet;

namespace SonarQube.Plugins.Roslyn
{
    /// <summary>
    /// Encapsulates the interactions with NuGet
    /// </summary>
    public interface INuGetPackageHandler
    {
        string LocalCacheRoot { get; }

        /// <summary>
        /// Attempts to fetch the specified package
        /// </summary>
        /// <param name="packageId">The package identifier</param>
        /// <param name="version">The version of the package to download. Can be null,
        /// in which case the most recent version will be downloaded (which could be a pre-release version).
        /// </param>
        /// <returns>A reference to the package, or null if the package could not be located</returns>
        IPackage FetchPackage(string packageId, SemanticVersion version);

        /// <summary>
        /// Returns the closure of packages required by the specified package
        /// that have been installed locally
        /// </summary>
        IEnumerable<IPackage> GetInstalledDependencies(IPackage package);

        /// <summary>
        /// Returns the local directory containing the specified package
        /// </summary>
        /// <remarks>Assumes that the package has already been fetched</remarks>
        string GetLocalPackageRootDirectory(IPackage package);
    }
}