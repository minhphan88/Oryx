﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Oryx.Common;
using Microsoft.Oryx.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Oryx.BuildImage.Tests
{
    public class HugoSampleAppsTest : HugoSampleAppsTestBase
    {
        public HugoSampleAppsTest(ITestOutputHelper output) : base(output)
        {
        }

        public static TheoryData<string> ImageNameData
        {
            get
            {
                var data = new TheoryData<string>();
                data.Add(Settings.BuildImageName);
                data.Add(Settings.LtsVersionsBuildImageName);
                var imageTestHelper = new ImageTestHelper();
                data.Add(imageTestHelper.GetAzureFunctionsJamStackBuildImage());
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ImageNameData))]
        public void GeneratesScript_AndBuilds(string buildImageName)
        {
            // Arrange
            var volume = CreateSampleAppVolume();
            var appDir = volume.ContainerDir;
            var appOutputDir = "/tmp/app-output";
            var script = new ShellScriptBuilder()
                .AddBuildCommand($"{appDir} -i /tmp/int -o {appOutputDir}")
                .AddFileExistsCheck($"{appOutputDir}/public/index.xml")
                .ToString();

            // Act
            var result = _dockerCli.Run(new DockerRunArguments
            {
                ImageId = buildImageName,
                Volumes = new List<DockerVolume> { volume },
                CommandToExecuteOnRun = "/bin/bash",
                CommandArguments = new[] { "-c", script }
            });

            // Assert
            RunAsserts(
                () =>
                {
                    Assert.True(result.IsSuccess);
                    Assert.Contains("Using Hugo version:", result.StdOut);
                },
                result.GetDebugInfo());
        }

        [Theory]
        [InlineData("hugo-sample")]
        [InlineData("hugo-sample-json")]
        [InlineData("hugo-sample-yaml")]
        public void CanBuildHugoAppHavingDifferentConfigFileTypes(string appName)
        {
            // Arrange
            var volume = CreateSampleAppVolume(appName);
            var appDir = volume.ContainerDir;
            var appOutputDir = "/tmp/app-output";
            var script = new ShellScriptBuilder()
                .AddBuildCommand($"{appDir} -i /tmp/int -o {appOutputDir}")
                .AddFileExistsCheck($"{appOutputDir}/public/index.xml")
                .ToString();

            // Act
            var result = _dockerCli.Run(new DockerRunArguments
            {
                ImageId = _imageHelper.GetGitHubActionsBuildImage(),
                Volumes = new List<DockerVolume> { volume },
                CommandToExecuteOnRun = "/bin/bash",
                CommandArguments = new[] { "-c", script }
            });

            // Assert
            RunAsserts(
                () =>
                {
                    Assert.True(result.IsSuccess);
                    Assert.Contains("Using Hugo version:", result.StdOut);
                },
                result.GetDebugInfo());
        }

        [Fact]
        public void CanBuildHugoAppHavingPackageJson_ByExplicitlySpecifyingHugoPlatform()
        {
            // Idea is here that even though the app has a package.json, a user can explicitly choose for Hugo
            // platform to take care of build.

            // Arrange
            var volume = CreateSampleAppVolume("hugo-sample-with-packagejson");
            var appDir = volume.ContainerDir;
            var appOutputDir = "/tmp/app-output";
            var script = new ShellScriptBuilder()
                .AddBuildCommand($"{appDir} -i /tmp/int -o {appOutputDir} --platform hugo")
                .AddFileExistsCheck($"{appOutputDir}/public/index.xml")
                .ToString();

            // Act
            var result = _dockerCli.Run(new DockerRunArguments
            {
                ImageId = _imageHelper.GetGitHubActionsBuildImage(),
                Volumes = new List<DockerVolume> { volume },
                CommandToExecuteOnRun = "/bin/bash",
                CommandArguments = new[] { "-c", script }
            });

            // Assert
            RunAsserts(
                () =>
                {
                    Assert.True(result.IsSuccess);
                    Assert.Contains("Using Hugo version:", result.StdOut);
                },
                result.GetDebugInfo());
        }
    }
}
