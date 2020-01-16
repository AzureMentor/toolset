// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Tools.Test.Utilities
{
    public static class RepoDirectoriesProvider
    {
        public readonly static string RepoRoot;

        public readonly static string TestWorkingFolder;
        public readonly static string DotnetUnderTest;
        public readonly static string DotnetRidUnderTest;
        public readonly static string SdkFolderUnderTest;
        public readonly static string TestPackages;

        //  For tests which want the global packages folder isolated in the repo, but can share
        //  it with other tests
        public readonly static string TestGlobalPackagesFolder;

        public const string DotnetHomeVariableName = "DOTNET_CLI_HOME";

        static RepoDirectoriesProvider()
        {
            //  Show verbose debugging output for tests
            Cli.Utils.CommandContext.SetVerbose(true);
            Cli.Utils.Reporter.Reset();


#if NET451
            string directory = AppDomain.CurrentDomain.BaseDirectory;
#else
            string directory = AppContext.BaseDirectory;
#endif

            while (directory != null)
            {
                var gitDirOrFile = Path.Combine(directory, ".git");
                if (Directory.Exists(gitDirOrFile) || File.Exists(gitDirOrFile))
                {
                    break;
                }
                directory = Directory.GetParent(directory)?.FullName;
            }

            RepoRoot = directory;

            TestWorkingFolder = Environment.GetEnvironmentVariable("CORESDK_TEST_FOLDER");

            DotnetUnderTest = Environment.GetEnvironmentVariable("DOTNET_UNDER_TEST");
            string dotnetExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";

            string artifactsFolder;
            string arcadeContainer = Environment.GetEnvironmentVariable("ARCADE_CONTAINER");
            if (string.IsNullOrEmpty(arcadeContainer))
            {
                artifactsFolder = Path.Combine(RepoRoot, "artifacts");
            }
            else
            {
                artifactsFolder = Path.Combine(RepoRoot, "artifacts-" + arcadeContainer);
            }
            
            

            if (string.IsNullOrEmpty(DotnetUnderTest))
            {
                if (RepoRoot == null)
                {
                    DotnetUnderTest = "dotnet" + dotnetExtension;
                }
                else
                {
                    //                    string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent.Name;
#if DEBUG
                    string configuration = "Debug";
#else
                    string configuration = "Release";
#endif
                    DotnetUnderTest = Path.Combine(artifactsFolder, "bin", "redist", configuration, "dotnet", "dotnet" + dotnetExtension);
                    TestPackages = Path.Combine(artifactsFolder, "tmp", configuration, "testpackages");
                    if (string.IsNullOrEmpty(TestWorkingFolder))
                    {
                        TestWorkingFolder = Path.Combine(artifactsFolder, "tmp", configuration);
                    }
                }
            }

            TestGlobalPackagesFolder = Path.Combine(artifactsFolder, ".nuget", "packages");

            //  TODO: Resolve dotnet folder even if DotnetUnderTest doesn't have full path
            var sdkFolders = Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(DotnetUnderTest), "sdk"));
            sdkFolders.Length.Should().Be(1, "Only one SDK folder is expected in the layout");

            SdkFolderUnderTest = sdkFolders.Single();
            var versionFile = Path.Combine(SdkFolderUnderTest, ".version");

            var lines = File.ReadAllLines(versionFile);
            DotnetRidUnderTest = lines[2].Trim();

            //  Set up test hooks for in-process tests
            Environment.SetEnvironmentVariable(
                Cli.Utils.Constants.MSBUILD_EXE_PATH,
                Path.Combine(SdkFolderUnderTest, "MSBuild.dll"));

            Environment.SetEnvironmentVariable(
                "MSBuildSDKsPath",
                Path.Combine(SdkFolderUnderTest, "Sdks"));

            Cli.Utils.MSBuildForwardingAppWithoutLogging.MSBuildExtensionsPathTestHook = RepoDirectoriesProvider.SdkFolderUnderTest;


        }

    }
}
