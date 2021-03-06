﻿using System.Collections.Generic;
using Cake.Common.Tests.Fixtures;
using Cake.Common.Tests.Fixtures.Tools;
using Cake.Common.Tests.Fixtures.Tools.NuGet;
using Cake.Common.Tools.NuGet;
using Cake.Core;
using Cake.Core.IO;
using NSubstitute;
using Xunit;

namespace Cake.Common.Tests.Unit.Tools.NuGet.Update
{
    public sealed class NuGetUpdaterTests
    {
        public sealed class TheUpdateMethod
        {
            [Fact]
            public void Should_Throw_If_Target_File_Path_Is_Null()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.TargetFile = null;

                // When
                var result = Record.Exception(() => fixture.Update());

                // Then
                Assert.IsArgumentNullException(result, "targetFile");
            }

            [Fact]
            public void Should_Throw_If_Settings_Are_Null()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.Settings = null;

                // When
                var result = Record.Exception(() => fixture.Update());

                // Then
                Assert.IsArgumentNullException(result, "settings");
            }

            [Fact]
            public void Should_Throw_If_NuGet_Executable_Was_Not_Found()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.GivenDefaultToolDoNotExist();

                // When
                var result = Record.Exception(() => fixture.Update());

                // Then
                Assert.IsType<CakeException>(result);
                Assert.Equal("NuGet: Could not locate executable.", result.Message);
            }

            [Theory]
            [InlineData("C:/nuget/nuget.exe", "C:/nuget/nuget.exe")]
            [InlineData("./tools/nuget/nuget.exe", "/Working/tools/nuget/nuget.exe")]
            public void Should_Use_NuGet_Executable_From_Tool_Path_If_Provided(string toolPath, string expected)
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.Settings.ToolPath = toolPath;
                fixture.GivenCustomToolPathExist(expected);

                // When
                fixture.Update();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Is<FilePath>(p => p.FullPath == expected),
                    Arg.Any<ProcessSettings>());
            }

            [Fact]
            public void Should_Throw_If_Process_Was_Not_Started()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.ProcessRunner.Start(Arg.Any<FilePath>(), Arg.Any<ProcessSettings>()).Returns((IProcess)null);

                // When
                var result = Record.Exception(() => fixture.Update());

                // Then
                Assert.IsType<CakeException>(result);
                Assert.Equal("NuGet: Process was not started.", result.Message);
            }

            [Fact]
            public void Should_Throw_If_Process_Has_A_Non_Zero_Exit_Code()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.Process.GetExitCode().Returns(1);

                // When
                var result = Record.Exception(() => fixture.Update());

                // Then
                Assert.IsType<CakeException>(result);
                Assert.Equal("NuGet: Process returned an error.", result.Message);
            }

            [Fact]
            public void Should_Find_NuGet_Executable_If_Tool_Path_Not_Provided()
            {
                // Given
                var fixture = new NuGetUpdateFixture();

                // When
                fixture.Update();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Is<FilePath>(p => p.FullPath == "/Working/tools/NuGet.exe"),
                    Arg.Any<ProcessSettings>());
            }

            [Fact]
            public void Should_Add_Mandatory_Arguments()
            {
                // Given
                var fixture = new NuGetUpdateFixture();

                // When
                fixture.Update();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "update \"/Working/packages.config\" -NonInteractive"));
            }

            [Fact]
            public void Should_Add_Packages_If_Specified()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.Settings.Id = new List<string> { "A", "B" };

                // When
                fixture.Update();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "update \"/Working/packages.config\" -Id \"A;B\" -NonInteractive"));
            }

            [Fact]
            public void Should_Add_Safe_Flag_If_Set()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.Settings.Safe = true;

                // When
                fixture.Update();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "update \"/Working/packages.config\" -Safe -NonInteractive"));
            }

            [Fact]
            public void Should_Add_Prerelease_Flag_If_Set()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.Settings.Prerelease = true;

                // When
                fixture.Update();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "update \"/Working/packages.config\" -Prerelease -NonInteractive"));
            }

            [Theory]
            [InlineData(NuGetVerbosity.Detailed, "update \"/Working/packages.config\" -Verbosity detailed -NonInteractive")]
            [InlineData(NuGetVerbosity.Normal, "update \"/Working/packages.config\" -Verbosity normal -NonInteractive")]
            [InlineData(NuGetVerbosity.Quiet, "update \"/Working/packages.config\" -Verbosity quiet -NonInteractive")]
            public void Should_Add_Verbosity_To_Arguments_If_Set(NuGetVerbosity verbosity, string expected)
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.Settings.Verbosity = verbosity;

                // When
                fixture.Update();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == expected));
            }

            [Fact]
            public void Should_Add_Sources_To_Arguments_If_Set()
            {
                // Given
                var fixture = new NuGetUpdateFixture();
                fixture.Settings.Source = new[] { "A", "B", "C" };

                // When
                fixture.Update();

                // Then
                fixture.ProcessRunner.Received(1).Start(
                    Arg.Any<FilePath>(), Arg.Is<ProcessSettings>(p =>
                        p.Arguments.Render() == "update \"/Working/packages.config\" -Source \"A;B;C\" -NonInteractive"));
            }
        }
    }
}
