#load lib.cake
#load acceptance.cake
#tool nuget:?package=vswhere

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.11.2";
var modifier = "";

var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = version + modifier + dbgSuffix;

if (BuildSystem.IsRunningOnAppVeyor)
{
    var tag = AppVeyor.Environment.Repository.Tag;

    if (tag.IsTag)
    {
        packageVersion = tag.Name;
    }
    else
    {
        var buildNumber = AppVeyor.Environment.Build.Number.ToString("00000");
        var branch = AppVeyor.Environment.Repository.Branch.Replace(".", "").Replace("/", "");
        var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;

        if (branch == "master" && !isPullRequest)
        {
            packageVersion = version + "-dev-" + buildNumber + dbgSuffix;
        }
        else
        {
            var suffix = "-ci-" + buildNumber + dbgSuffix;

            if (isPullRequest)
                suffix += "-pr-" + AppVeyor.Environment.PullRequest.Number;
            else
                suffix += "-" + System.Text.RegularExpressions.Regex.Replace(branch, "[^0-9A-Za-z-]+", "-");

            // Nuget limits "special version part" to 20 chars. Add one for the hyphen.
            if (suffix.Length > 21)
                suffix = suffix.Substring(0, 21);

            packageVersion = version + suffix;
        }
    }

    AppVeyor.UpdateBuildVersion(packageVersion);
}

var packageName = "NUnit3TestAdapter-" + packageVersion;

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

// Directories
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var PACKAGE_IMAGE_DIR = PACKAGE_DIR + packageName + "/";
var SRC_DIR = PROJECT_DIR + "src/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";

var ADAPTER_PROJECT = SRC_DIR + "NUnitTestAdapter/NUnit.TestAdapter.csproj";

var ADAPTER_BIN_DIR_NET35 = SRC_DIR + $"NUnitTestAdapter/bin/{configuration}/net35/";
var ADAPTER_BIN_DIR_NETCOREAPP10 = SRC_DIR + $"NUnitTestAdapter/bin/{configuration}/netcoreapp1.0/";

var BIN_DIRS = new [] {
    PROJECT_DIR + "src/empty-assembly/bin",
    PROJECT_DIR + "src/mock-assembly/bin",
    PROJECT_DIR + "src/NUnit3TestAdapterInstall/bin",
    PROJECT_DIR + "src/NUnit3TestAdapter/bin",
    PROJECT_DIR + "src/NUnit3TestAdapterTests/bin",
};

// Solution
var ADAPTER_SOLUTION = PROJECT_DIR + "NUnit3TestAdapter.sln";

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    foreach(var dir in BIN_DIRS)
        CleanDirectory(dir);
});

//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("NuGetRestore")
    .Does(() =>
{
    Information("Restoring NuGet Packages for the Adapter Solution");
    MSBuild(ADAPTER_SOLUTION, new MSBuildSettings
    {
        Configuration = configuration,
        Verbosity = Verbosity.Minimal,
        ToolVersion = MSBuildToolVersion.VS2017
    }.WithTarget("Restore"));
});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("NuGetRestore")
    .Does(() =>
    {
        // Find MSBuild for Visual Studio 2017
        DirectoryPath vsLatest  = VSWhereLatest();
        FilePath msBuildPathX64 = (vsLatest==null) ? null
                                    : vsLatest.CombineWithFilePath("./MSBuild/15.0/Bin/MSBuild.exe");

        Information("Building using MSBuild at " + msBuildPathX64);
        Information("Configuration is:"+configuration);

        MSBuild(ADAPTER_SOLUTION, new MSBuildSettings
        {
            Configuration = configuration,
            ToolPath = msBuildPathX64,
            ToolVersion = MSBuildToolVersion.VS2017,
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["PackageVersion"] = packageVersion
            }
        });
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

string GetTestAssemblyPath(string framework)
{
    return SRC_DIR + $"NUnitTestAdapterTests/bin/{configuration}/{framework}/NUnit.VisualStudio.TestAdapter.Tests.dll";
}

foreach (var (framework, vstestFramework, adapterDir) in new[] {
    ("net46", "Framework45", ADAPTER_BIN_DIR_NET35),
    ("netcoreapp1.0", "FrameworkCore10", ADAPTER_BIN_DIR_NETCOREAPP10)
})
{
    Task($"VSTest-{framework}")
        .IsDependentOn("Build")
        .Does(() =>
        {
            VSTest(GetTestAssemblyPath(framework), new VSTestSettings
            {
                TestAdapterPath = adapterDir,
                // Enables the tests to run against the correct version of Microsoft.VisualStudio.TestPlatform.ObjectModel.dll.
                // (The DLL they are compiled against depends on VS2012 at runtime.)
                SettingsFile = File("DisableAppDomain.runsettings"),

                // https://github.com/cake-build/cake/issues/2077
                #tool Microsoft.TestPlatform
                ToolPath = Context.Tools.Resolve("vstest.console.exe")
            });
        });

    Task($"DotnetTest-{framework}")
        .IsDependentOn("Build")
        .Does(() =>
        {
            DotNetCoreTest(SRC_DIR + "NUnitTestAdapterTests/NUnit.TestAdapter.Tests.csproj", new DotNetCoreTestSettings
            {
                Configuration = configuration,
                Framework = framework,
                NoBuild = true,
                TestAdapterPath = adapterDir,
                Settings = File("DisableAppDomain.runsettings")
            });
        });
}

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("CreatePackageDir")
    .Does(() =>
    {
        CreateDirectory(PACKAGE_DIR);
    });

Task("CreateWorkingImage")
    .IsDependentOn("CreatePackageDir")
    .Does(() =>
    {
        CreateDirectory(PACKAGE_IMAGE_DIR);
        CleanDirectory(PACKAGE_IMAGE_DIR);

        CopyFileToDirectory("LICENSE.txt", PACKAGE_IMAGE_DIR);

        // dotnet publish doesn't work for .NET 3.5
        var net35Files = new FilePath[]
        {
            ADAPTER_BIN_DIR_NET35 + "NUnit3.TestAdapter.dll",
            ADAPTER_BIN_DIR_NET35 + "NUnit3.TestAdapter.pdb",
            ADAPTER_BIN_DIR_NET35 + "nunit.engine.dll",
            ADAPTER_BIN_DIR_NET35 + "nunit.engine.api.dll"
        };

        var net35Dir = PACKAGE_IMAGE_DIR + "build/net35";
        CreateDirectory(net35Dir);
        CopyFiles(net35Files, net35Dir);
        CopyFileToDirectory("nuget/net35/NUnit3TestAdapter.props", net35Dir);

        var netcoreDir = PACKAGE_IMAGE_DIR + "build/netcoreapp1.0";
        DotNetCorePublish(ADAPTER_PROJECT, new DotNetCorePublishSettings
        {
            Configuration = configuration,
            OutputDirectory = netcoreDir,
            Framework = "netcoreapp1.0"
        });
        CopyFileToDirectory("nuget/netcoreapp1.0/NUnit3TestAdapter.props", netcoreDir);
    });

Task("PackageZip")
    .IsDependentOn("CreateWorkingImage")
    .Does(() =>
    {
        Zip(PACKAGE_IMAGE_DIR, File(PACKAGE_DIR + packageName + ".zip"));
    });

Task("PackageNuGet")
    .IsDependentOn("CreateWorkingImage")
    .Does(() =>
    {
        NuGetPack("nuget/NUnit3TestAdapter.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = PACKAGE_IMAGE_DIR,
            OutputDirectory = PACKAGE_DIR
        });
    });

Task("PackageVsix")
    .IsDependentOn("CreatePackageDir")
    .Does(() =>
    {
        CopyFile(
            BIN_DIR + "NUnit3TestAdapter.vsix",
            PACKAGE_DIR + packageName + ".vsix");
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Build");

Task("Test")
    .IsDependentOn("VSTest-net46")
    .IsDependentOn("VSTest-netcoreapp1.0")
    .IsDependentOn("DotnetTest-net46")
    .IsDependentOn("DotnetTest-netcoreapp1.0");

Task("Package")
    .IsDependentOn("PackageZip")
    .IsDependentOn("PackageNuGet")
    .IsDependentOn("PackageVsix");

Task("Appveyor")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .IsDependentOn("Acceptance");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
