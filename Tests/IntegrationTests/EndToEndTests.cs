/*
 * SonarQube Roslyn SDK
 * Copyright (C) 2015-2017 SonarSource SA
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
using SonarQube.Plugins.Test.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SonarQube.Plugins.IntegrationTests
{
    // End-to-end tests that invoke the generator executable

    [TestClass]
    public class EndToEndTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Execute_MissingArgs_Fails()
        {
            var exeFilePath = GetExeFilePath();

            using (var runner = ProcessRunner.Start(exeFilePath, String.Empty))
            {
                await runner.WaitForExitAsync();

                runner.Process.ExitCode.Should().Be(1);
                runner.ErrorMessages.Count.Should().BeGreaterThan(0);
            }
        }

        [TestMethod]
        [DataRow("RoslynAnalyzer10:1.0.0")]
        [DataRow("RoslynAnalyzer11:1.0.0")]
        [DataRow("wintellect.analyzers")]
        //[DataRow("RoslynV298")]
        //[DataRow("RoslynV333")]
        public async Task Execute_ExampleAnalyzers_Succeeds(string analyzerArg)
        {
            var exeFilePath = GetExeFilePath();

            var examplePkgDir = GetExampleAnalyzerPkgDir();

            var testOutDir = TestUtils.CreateTestDirectory(TestContext, Guid.NewGuid().ToString());

            var args = $"/customnugetrepo:\"{examplePkgDir}\" /o:\"{testOutDir}\" /a:{analyzerArg}";

            using (var runner = ProcessRunner.Start(exeFilePath, args))
            {
                await runner.WaitForExitAsync();

                runner.Process.HasExited.Should().BeTrue();
                runner.Process.ExitCode.Should().Be(0);

                var actualFiles = Directory.GetFiles(testOutDir, "*.*", SearchOption.TopDirectoryOnly);
                actualFiles.Length.Should().Be(2);
                actualFiles.Count(x => x.EndsWith(".rules.template.xml")).Should().Be(1);
                actualFiles.Count(x => x.EndsWith(".jar")).Should().Be(1);
            }
        }

        private static string GetExeFilePath() => typeof(SonarQube.Plugins.Roslyn.Program).Assembly.Location;

        private static string GetAssemblyLocationFromType(Type type) =>
            Path.GetDirectoryName(type.Assembly.Location);

        private static string GetExampleAnalyzerPkgDir() =>
            Path.Combine(GetAssemblyLocationFromType(typeof(EndToEndTests)),
                "SampleNuGetPkgs");

        private sealed class ProcessRunner : IDisposable
        {
            private const int ExeTimeoutInMs = 20000;

            private bool disposedValue;
            private Process process;
            public static ProcessRunner Start(string exeFilePath, string cmdLineArgs)
                => new ProcessRunner(exeFilePath, cmdLineArgs);

            public Process Process => process;

            public IList<string> OutputMessages { get; } = new List<string>();
            public IList<string> ErrorMessages { get; } = new List<string>();

            public Task WaitForExitAsync() => Task.FromResult(Process.WaitForExit(ExeTimeoutInMs));

            private ProcessRunner(string exeFilePath, string cmdLineArgs)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exeFilePath,
                    Arguments = cmdLineArgs,
                    UseShellExecute = false, // required if we want to capture the error output
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                process = Process.Start(psi);

                process.ErrorDataReceived += ErrorDataReceived;
                process.OutputDataReceived += OutputDataReceived;

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            private void OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                OutputMessages.Add(e.Data);
                Console.WriteLine("[EXE Output]" + e.Data);
            }

            private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                ErrorMessages.Add(e.Data);
                Console.WriteLine("[EXE Error]" + e.Data);
            }

            private void Dispose(bool disposing)
            {
                if (disposedValue)
                {
                    return;
                }

                if (disposing && process != null)
                {
                    process.ErrorDataReceived -= OutputDataReceived;
                    process.OutputDataReceived -= ErrorDataReceived;
                    process.Dispose();
                    process = null;
                }

                disposedValue = true;
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
