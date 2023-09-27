#tool vswhere&version=3.1.1
#tool Microsoft.TestPlatform&version=17.7.2

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////


var version = "4.6.0";

var modifier = "-beta.1";

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

var NETCOREAPP_TFM = "netcoreapp3.1";

var ADAPTER_BIN_DIR_NET462 = SRC_DIR + $"NUnitTestAdapter/bin/{configuration}/net462/";
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

Task("CleanPackages")
    .Does(()=>
    {
    CleanDirectory(PACKAGE_DIR);
    });

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Does(() =>
    {
        if (IsRunningOnWindows())
        {
            // Workaround for https://github.com/cake-build/cake/issues/2128
            // cannot find pure preview installations of visual studio
            var vsInstallation =
                VSWhereLatest(new VSWhereLatestSettings { Requires = "Microsoft.Component.MSBuild" })
                ?? VSWhereLatest(new VSWhereLatestSettings { Requires = "Microsoft.Component.MSBuild", IncludePrerelease = true });
            if(vsInstallation == null)
            {
                throw new Exception($"Failed to find any Visual Studio version");
            }
            
            FilePath msBuildPath = vsInstallation.CombineWithFilePath(@"MSBuild\Current\Bin\MSBuild.exe");
            if (!FileExists(msBuildPath))
            {
                msBuildPath = vsInstallation.CombineWithFilePath(@"MSBuild\15.0\Bin\MSBuild.exe");
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
        }
        else
        {
            var settings = new DotNetCoreBuildSettings
            {
                Configuration = configuration,
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["PackageVersion"] = packageVersion
                }
            };

            DotNetCoreBuild(@"src\NUnitTestAdapterTests", settings);
            DotNetCoreBuild(@"src\NUnit.TestAdapter.Tests.Acceptance", settings);
        }
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

string GetTestAssemblyPath(string framework)
{
    return SRC_DIR + $"NUnitTestAdapterTests/bin/{configuration}/{framework}/NUnit.VisualStudio.TestAdapter.Tests.dll";
}

foreach (var (framework, vstestFramework, adapterDir) in new[] {
    ("net462", "Framework45", ADAPTER_BIN_DIR_NET462),
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

        CopyFileToDirectory("LICENSE", PACKAGE_IMAGE_DIR);

        // dotnet publish doesn't work for .NET 3.5
        var net462Files = new FilePath[]
        {
            ADAPTER_BIN_DIR_NET462 + "NUnit3.TestAdapter.dll",
            ADAPTER_BIN_DIR_NET462 + "NUnit3.TestAdapter.pdb",
            ADAPTER_BIN_DIR_NET462 + "nunit.engine.dll",
            ADAPTER_BIN_DIR_NET462 + "nunit.engine.api.dll",
            ADAPTER_BIN_DIR_NET462 + "nunit.engine.core.dll",
            ADAPTER_BIN_DIR_NET462 + "testcentric.engine.metadata.dll"
        };

        var net462Dir = PACKAGE_IMAGE_DIR + "build/net462";
        CreateDirectory(net462Dir);
        CopyFiles(net462Files, net462Dir);
        CopyFileToDirectory("nuget/net462/NUnit3TestAdapter.props", net462Dir);

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

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Build");

Task("Test")
    .IsDependentOn("VSTest-net462")
    .IsDependentOn("VSTest-" + NETCOREAPP_TFM)
    .IsDependentOn("DotnetTest-net462")
    .IsDependentOn("DotnetTest-" + NETCOREAPP_TFM);

Task("Package")
    .IsDependentOn("PackageZip")
    .IsDependentOn("PackageNuGet");

Task("QuickRelease")
    .IsDependentOn("Build")
    .IsDependentOn("Package");

Task("Release")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .IsDependentOn("Acceptance");


Task("Acceptance")
    .IsDependentOn("Build")
    .IsDependentOn("PackageNuGet")
    .Description("Ensures that known project configurations can use the produced NuGet package to restore, build, and run tests.")
    .Does(() =>
    {
        // Target framework specified here should be exactly the same as the one in the acceptance project file.
        var targetframework = "net48";
        var testAssembly = SRC_DIR + $"NUnit.TestAdapter.Tests.Acceptance/bin/{configuration}/{targetframework}/NUnit.VisualStudio.TestAdapter.Tests.Acceptance.dll";

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
