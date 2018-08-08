/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2018 SonarSource SA
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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarQube.Plugins.PluginGeneratorTests
{
    [TestClass]
    public class JarManifestReaderTests
    {
        [TestMethod]
        public void ReadSingleAndMultiLines()
        {
            // Arrange
            var text = @"Manifest-Version: 1.0

Plugin-Dependencies: META-INF/lib/jsr305-1.3.9.jar META-INF/lib/common
 s-io-2.6.jar META-INF/lib/stax2-api-3.1.4.jar META-INF/lib/staxmate-2
 .0.1.jar META-INF/lib/stax-api-1.0.1.jar
Plugin-SourcesUrl: https://github.com/SonarSource-VisualStudio/sonarqu
 be-roslyn-sdk-template-plugin


";
            // Act
            var jarReader = new JarManifestReader(text);

            // Assert
            jarReader.FindValue("Manifest-Version").Should().Be("1.0");

            // Multi-line value should be concatenated correctly
            jarReader.FindValue("Plugin-Dependencies").Should().Be("META-INF/lib/jsr305-1.3.9.jar META-INF/lib/commons-io-2.6.jar META-INF/lib/stax2-api-3.1.4.jar META-INF/lib/staxmate-2.0.1.jar META-INF/lib/stax-api-1.0.1.jar");

            // Multi-line with blank lines after - blanks ignored
            jarReader.FindValue("Plugin-SourcesUrl").Should().Be(@"https://github.com/SonarSource-VisualStudio/sonarqube-roslyn-sdk-template-plugin");

            // Not case-sensitive
            jarReader.FindValue("MANIFEST-VERSION").Should().Be("1.0");
        }

        [TestMethod]
        public void MissingSeparator_Throws()
        {
            // Arrange
            var text = @"Manifest-Version: 1.0
Line without a separator";

            // Act
            Action act = () => new JarManifestReader(text);

            // Assert
            act.Should().ThrowExactly<InvalidOperationException>().And.Message.Should().Be("Manifest file is not valid - line does not contain a key-value separator: 'Line without a separator'");
        }

        [TestMethod]
        public void Get_MissingSetting_Throws()
        {
            // Arrange
            var text = @"Manifest-Version: 1.0";
            var jarReader = new JarManifestReader(text);

            // Act
            Action act = () => jarReader.GetValue("missing-key");

            // Assert
            act.Should().ThrowExactly<InvalidOperationException>().And.Message.Should().Be("The expected setting was not found in the manifest file: missing-key");
        }
    }
}
