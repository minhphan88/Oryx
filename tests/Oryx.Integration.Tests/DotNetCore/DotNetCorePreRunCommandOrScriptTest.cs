// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Microsoft.Oryx.BuildScriptGeneratorCli;
using Microsoft.Oryx.Common;
using Microsoft.Oryx.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Oryx.Integration.Tests
{
    [Trait("category", "dotnetcore")]
    public class DotNetCorePreRunCommandOrScriptTest : DotNetCoreEndToEndTestsBase
    {
        private readonly string RunScriptPath = DefaultStartupFilePath;
        private readonly string RunScriptTempPath = "/tmp/startup_temp.sh";
        private readonly string RunScriptPreRunPath = "/tmp/startup_prerun.sh";

        public DotNetCorePreRunCommandOrScriptTest(ITestOutputHelper output, TestTempDirTestFixture testTempDirTestFixture)
            : base(output, testTempDirTestFixture)
        {
        }

        [Fact]
        public async Task CanBuildAndRun_NetCore31MvcApp_UsingPreRunCommand_WithDynamicInstall()
        {
            // Arrange
            var runtimeVersion = "3.1";
            var appName = NetCoreApp31MvcApp;
            var hostDir = Path.Combine(_hostSamplesDir, "DotNetCore", appName);
            var volume = DockerVolume.CreateMirror(hostDir);
            var appDir = volume.ContainerDir;
            var appOutputDir = $"{appDir}/myoutputdir";
            var buildImageScript = new ShellScriptBuilder()
               .SetEnvironmentVariable(
                    SdkStorageConstants.SdkStorageBaseUrlKeyName,
                    SdkStorageConstants.DevSdkStorageBaseUrl)
               .AddCommand(
                $"oryx build {appDir} --platform dotnet --platform-version {runtimeVersion} -o {appOutputDir}")
               .ToString();

            // split run script to test pre-run command before running the app.
            var runtimeImageScript = new ShellScriptBuilder()
                .SetEnvironmentVariable(SettingsKeys.EnableDynamicInstall, true.ToString())
                .SetEnvironmentVariable(
                    SdkStorageConstants.SdkStorageBaseUrlKeyName,
                    SdkStorageConstants.DevSdkStorageBaseUrl)

                .SetEnvironmentVariable(FilePaths.PreRunCommandEnvVarName,
                    $"\"touch {appOutputDir}/_test_file.txt\ntouch {appOutputDir}/_test_file_2.txt\"")

                .AddCommand($"oryx create-script -appPath {appOutputDir} -output {RunScriptPath} -bindPort {ContainerPort}")

                .AddCommand($"LINENUMBER=\"$(grep -n '# End of pre-run' {RunScriptPath} | cut -f1 -d:)\"")
                .AddCommand($"eval \"head -n +${{LINENUMBER}} {RunScriptPath} > {RunScriptPreRunPath}\"")
                .AddCommand($"chmod +x {RunScriptPreRunPath}")
                .AddCommand($"LINENUMBERPLUSONE=\"$(expr ${{LINENUMBER}} + 1)\"")
                .AddCommand($"eval \"tail -n +${{LINENUMBERPLUSONE}} {RunScriptPath} > {RunScriptTempPath}\"")
                .AddCommand($"mv {RunScriptTempPath} {RunScriptPath}")
                .AddCommand($"head -n +1 {RunScriptPreRunPath} | cat - {RunScriptPath} > {RunScriptTempPath}")
                .AddCommand($"mv {RunScriptTempPath} {RunScriptPath}")
                .AddCommand($"chmod +x {RunScriptPath}")
                .AddCommand($"unset LINENUMBER")
                .AddCommand($"unset LINENUMBERPLUSONE")
                .AddCommand(RunScriptPreRunPath)
                .AddFileExistsCheck($"{appOutputDir}/_test_file.txt")
                .AddFileExistsCheck($"{appOutputDir}/_test_file_2.txt")
                .AddCommand(RunScriptPath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                NetCoreApp31MvcApp,
                _output,
                new[] { volume },
                _imageHelper.GetGitHubActionsBuildImage(),
                "/bin/sh",
                new[]
                {
                    "-c",
                    buildImageScript
                },
                _imageHelper.GetRuntimeImage("dotnetcore", "dynamic"),
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    runtimeImageScript
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Welcome to ASP.NET Core MVC!", data);
                });
        }

        [Fact]
        public async Task CanBuildAndRun_NetCore31MvcApp_UsingPreRunScript_WithDynamicInstall()
        {
            // Arrange
            var runtimeVersion = "3.1";
            var appName = NetCoreApp31MvcApp;
            var hostDir = Path.Combine(_hostSamplesDir, "DotNetCore", appName);
            var volume = DockerVolume.CreateMirror(hostDir);
            var appDir = volume.ContainerDir;
            var appOutputDir = $"{appDir}/myoutputdir";
            var preRunScriptPath = $"{appOutputDir}/prerunscript.sh";
            var buildImageScript = new ShellScriptBuilder()
               .SetEnvironmentVariable(
                    SdkStorageConstants.SdkStorageBaseUrlKeyName,
                    SdkStorageConstants.DevSdkStorageBaseUrl)
               .AddCommand(
                $"oryx build {appDir} --platform dotnet --platform-version {runtimeVersion} -o {appOutputDir}")
               .ToString();


            // split run script to test pre-run command and then run the app
            var runtimeImageScript = new ShellScriptBuilder()
                .SetEnvironmentVariable(SettingsKeys.EnableDynamicInstall, true.ToString())
                .SetEnvironmentVariable(
                    SdkStorageConstants.SdkStorageBaseUrlKeyName,
                    SdkStorageConstants.DevSdkStorageBaseUrl)

                .SetEnvironmentVariable(FilePaths.PreRunCommandEnvVarName, $"\"touch '{appOutputDir}/_test_file_2.txt' && {preRunScriptPath}\"")
                .AddCommand($"touch {preRunScriptPath}")
                .AddFileExistsCheck(preRunScriptPath)
                .AddCommand($"echo \"touch {appOutputDir}/_test_file.txt\" > {preRunScriptPath}")
                .AddStringExistsInFileCheck($"touch {appOutputDir}/_test_file.txt", $"{preRunScriptPath}")
                .AddCommand($"chmod +x {preRunScriptPath}")

                .AddCommand($"oryx create-script -appPath {appOutputDir} -output {RunScriptPath} -bindPort {ContainerPort}")

                .AddCommand($"LINENUMBER=\"$(grep -n '# End of pre-run' {RunScriptPath} | cut -f1 -d:)\"")
                .AddCommand($"eval \"head -n +${{LINENUMBER}} {RunScriptPath} > {RunScriptPreRunPath}\"")
                .AddCommand($"chmod +x {RunScriptPreRunPath}")
                .AddCommand($"LINENUMBERPLUSONE=\"$(expr ${{LINENUMBER}} + 1)\"")
                .AddCommand($"eval \"tail -n +${{LINENUMBERPLUSONE}} {RunScriptPath} > {RunScriptTempPath}\"")
                .AddCommand($"mv {RunScriptTempPath} {RunScriptPath}")
                .AddCommand($"head -n +1 {RunScriptPreRunPath} | cat - {RunScriptPath} > {RunScriptTempPath}")
                .AddCommand($"mv {RunScriptTempPath} {RunScriptPath}")
                .AddCommand($"chmod +x {RunScriptPath}")
                .AddCommand($"unset LINENUMBER")
                .AddCommand($"unset LINENUMBERPLUSONE")
                .AddCommand(RunScriptPreRunPath)
                .AddFileExistsCheck($"{appOutputDir}/_test_file.txt")
                .AddFileExistsCheck($"{appOutputDir}/_test_file_2.txt")
                .AddCommand(RunScriptPath)
                .ToString();

            await EndToEndTestHelper.BuildRunAndAssertAppAsync(
                NetCoreApp31MvcApp,
                _output,
                new[] { volume },
                _imageHelper.GetGitHubActionsBuildImage(),
                "/bin/sh",
                new[]
                {
                    "-c",
                    buildImageScript
                },
                _imageHelper.GetRuntimeImage("dotnetcore", "dynamic"),
                ContainerPort,
                "/bin/sh",
                new[]
                {
                    "-c",
                    runtimeImageScript
                },
                async (hostPort) =>
                {
                    var data = await _httpClient.GetStringAsync($"http://localhost:{hostPort}/");
                    Assert.Contains("Welcome to ASP.NET Core MVC!", data);
                });
        }
    }
}