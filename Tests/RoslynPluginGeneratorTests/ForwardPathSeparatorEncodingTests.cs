/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2022 SonarSource SA
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

using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarQube.Plugins.Roslyn.RoslynPluginGeneratorTests
{
    [TestClass]
    public class ForwardPathSeparatorEncodingTests
    {
        [DataTestMethod]
        [DataRow(@"C:\path\to\file", "C:/path/to/file")]
        [DataRow("C:/path/to/file", "C:/path/to/file")]
        [DataRow(@"path\to\file", "path/to/file")]
        [DataRow("path/to/file", "path/to/file")]
        public void GetBytes_PathHaveForwardSlashes(string path, string expected)
        {
            var sut = new ForwardPathSeparatorEncoding();
            
            var actual = sut.GetBytes(path);
            
            Encoding.UTF8.GetString(actual).Should().Be(expected);
        }
    }
}