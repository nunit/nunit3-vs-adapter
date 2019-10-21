#tool vswhere&version=2.7.1
#tool Microsoft.TestPlatform&version=16.3.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.15.1";
var modifier = "";

var dbgSuffix = configuration.ToLower() == "debug" ? "-dbg" : "";
var packageVersion = version + modifier + dbgSuffix;
Information("Packageversion: "+packageVersion);
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

var NETCOREAPP_TFM = "netcoreapp2.1";

var ADAPTER_BIN_DIR_NET35 = SRC_DIR + $"NUnitTestAdapter/bin/{configuration}/net35/";
var ADAPTER_BIN_DIR_NETCOREAPP = SRC_DIR + $"NUnitTestAdapter/bin/{configuration}/{NETCOREAPP_TFM}/";

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
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Does(() =>
    {
        // Find MSBuild for Visual Studio 2019 and newer
        DirectoryPath vsLatest = VSWhereLatest();
        FilePath msBuildPath = vsLatest?.CombineWithFilePath("./MSBuild/Current/Bin/MSBuild.exe");

        // Find MSBuild for Visual Studio 2017
        if (msBuildPath != null && !FileExists(msBuildPath))
            msBuildPath = vsLatest.CombineWithFilePath("./MSBuild/15.0/Bin/MSBuild.exe");

        // Have we found MSBuild yet?
        if ( !FileExists(msBuildPath) )
        {
            throw new Exception($"Failed to find MSBuild: {msBuildPath}");
        }

        Information("Building using MSBuild at " + msBuildPath);
        Information("Configuration is:"+configuration);

        MSBuild(ADAPTER_SOLUTION, new MSBuildSettings
        {
            Configuration = configuration,
            ToolPath = msBuildPath,
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["PackageVersion"] = packageVersion
            },
            Verbosity = Verbosity.Minimal,
            Restore = true
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
    (NETCOREAPP_TFM, NETCOREAPP_TFM, ADAPTER_BIN_DIR_NETCOREAPP)
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
                Logger = $"trx;LogFileName=VSTest-{framework}.trx"
            });

            PublishTestResults($"VSTest-{framework}.trx");
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
                Settings = File("DisableAppDomain.runsettings"),
                Logger = $"trx;LogFileName=DotnetTest-{framework}.trx",
                ResultsDirectory = MakeAbsolute(Directory("TestResults"))
            });

            PublishTestResults($"DotnetTest-{framework}.trx");
        });
}

void PublishTestResults(string fileName)
{
    if (EnvironmentVariable("TF_BUILD", false))
    {
        TFBuild.Commands.PublishTestResults(new TFBuildPublishTestResultsData
        {
            TestResultsFiles = { @"TestResults\" + fileName },
            TestRunTitle = fileName,
            TestRunner = TFTestRunnerType.VSTest,
            PublishRunAttachments = true,
            Configuration = configuration
        });
    }
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

        var netcoreDir = PACKAGE_IMAGE_DIR + "build/" + NETCOREAPP_TFM;
        DotNetCorePublish(ADAPTER_PROJECT, new DotNetCorePublishSettings
        {
            Configuration = configuration,
            OutputDirectory = netcoreDir,
            Framework = NETCOREAPP_TFM
        });
        CopyFileToDirectory($"nuget/{NETCOREAPP_TFM}/NUnit3TestAdapter.props", netcoreDir);
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
    .IsDependentOn("VSTest-" + NETCOREAPP_TFM)
    .IsDependentOn("DotnetTest-net46")
    .IsDependentOn("DotnetTest-" + NETCOREAPP_TFM);

Task("Package")
    .IsDependentOn("PackageZip")
    .IsDependentOn("PackageNuGet")
    .IsDependentOn("PackageVsix");

Task("Acceptance")
    .IsDependentOn("Build")
    .IsDependentOn("PackageNuGet")
    .Description("Ensures that known project configurations can use the produced NuGet package to restore, build, and run tests.")
    .Does(() =>
    {
        var testAssembly = SRC_DIR + $"NUnit.TestAdapter.Tests.Acceptance/bin/{configuration}/net472/NUnit.VisualStudio.TestAdapter.Tests.Acceptance.dll";

        var keepWorkspaces = Argument<bool?>("keep-workspaces", false) ?? true;

        VSTest(testAssembly, new VSTestSettings
        {
            SettingsFile = keepWorkspaces ? (FilePath)"KeepWorkspaces.runsettings" : null,
            Logger = "trx;LogFileName=Acceptance.trx"
        });

        PublishTestResults("Acceptance.trx");
    });

Task("CI")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .IsDependentOn("Acceptance");

Task("Appveyor")
    .IsDependentOn("CI");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
