#tool nuget:?package=vswhere

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "4.0.0";
var modifier = "-alpha1";

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
				suffix += "-" + branch;

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
var TOOLS_DIR = PROJECT_DIR + "tools/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";
var DEMO_BIN_DIR = PROJECT_DIR + "demo/NUnitTestDemo/bin/" + configuration + "/";

var BIN_DIRS = new [] {
    PROJECT_DIR + "src/empty-assembly/bin",
    PROJECT_DIR + "src/mock-assembly/bin",
    PROJECT_DIR + "src/NUnit3TestAdapterInstall/bin",
    PROJECT_DIR + "src/NUnit3TestAdapter/bin",
    PROJECT_DIR + "src/NUnit3TestAdapterTests/bin",
};

// Solutions
var ADAPTER_SOLUTION = PROJECT_DIR + "NUnit3TestAdapter.sln";
var DEMO_SOLUTION = PROJECT_DIR + "demo/NUnit3TestDemo.sln";

// Test Assemblies
var DEMO_TESTS = DEMO_BIN_DIR + "NUnit3TestDemo.dll";

var TEST_NET45 = SRC_DIR + "NUnitTestAdapterTests/bin/" + configuration + "/net45/NUnit.VisualStudio.TestAdapter.Tests.exe";
var TEST_PROJECT = SRC_DIR + "NUnitTestAdapterTests/NUnit.TestAdapter.Tests.csproj";

// Find MSBuild for Visual Studio 2017
DirectoryPath vsLatest  = VSWhereLatest();
FilePath msBuildPathX64 = (vsLatest==null) ? null
                            : vsLatest.CombineWithFilePath("./MSBuild/15.0/Bin/amd64/MSBuild.exe");

// Custom settings for VSTest
var VSTestCustomSettings = new VSTestSettings()
{
	ArgumentCustomization = args => args.Append("/TestAdapterPath:" + BIN_DIR)
};

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    foreach(var dir in BIN_DIRS)
        CleanDirectory(dir);
	CleanDirectory(DEMO_BIN_DIR);
});

//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("NuGetRestore")
    .Does(() =>
{
	NuGetRestore(DEMO_SOLUTION);
});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("NuGetRestore")
    .Does(() =>
    {
        var settings = new MSBuildSettings
        {
            Configuration = configuration,
            EnvironmentVariables = new Dictionary<string, string>(),
            NodeReuse = false,
            PlatformTarget = PlatformTarget.MSIL,
            ToolPath = msBuildPathX64,
            ToolVersion = MSBuildToolVersion.VS2017
        };
        settings.EnvironmentVariables.Add("PackageVersion", packageVersion);

        MSBuild(ADAPTER_SOLUTION, settings);
		MSBuild(DEMO_SOLUTION, settings);
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("TestAdapter")
	.IsDependentOn("Build")
	.Does(() =>
	{
        int result = StartProcess(TEST_NET45);
        if (result != 0)
            throw new Exception("TestAdapter failed");
	});

Task("TestAdapterNetCore")
	.IsDependentOn("Build")
	.Does(() =>
	{
        var settings = new DotNetCoreRunSettings
        {
            Framework = "netcoreapp1.0",
            Configuration = configuration
        };
		DotNetCoreRun(TEST_PROJECT, "", settings);
	});

Task("TestAdapterUsingVSTest")
	.IsDependentOn("Build")
	.Does(() =>
	{
		//VSTest(ADAPTER_TESTS, VSTestCustomSettings);
	});

Task("TestDemo")
	.IsDependentOn("Build")
	.Does(() =>
	{
		try
		{
			//VSTest(DEMO_TESTS, VSTestCustomSettings);
		}
		catch(Exception ex)
		{
			Information("\nNOTE: Demo tests failed as expected.");
			Information("This is normally not an error.\n");
		}
	});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("CreatePackageDir")
	.Does(() =>
	{
		CreateDirectory(PACKAGE_DIR);
	});

Task("PackageNuGet")
	.IsDependentOn("CreatePackageDir")
	.Does(() =>
	{
        var nuget = "NUnit3TestAdapter." + packageVersion + ".nupkg";
        var src   = "src/NUnitTestAdapter/bin/" + configuration + "/" + nuget;
        var dest  = PACKAGE_DIR + nuget;
        CopyFile(src, dest);
	});

Task("PackageVsix")
	.IsDependentOn("CreatePackageDir")
	.Does(() =>
	{
		//CopyFile(
		//	BIN_DIR + "NUnit3TestAdapter.vsix",
		//	PACKAGE_DIR + packageName + ".vsix");
	});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .IsDependentOn("Clean")
	.IsDependentOn("Build");

Task("Test")
	.IsDependentOn("TestAdapter")
	.IsDependentOn("TestAdapterNetCore")
	.IsDependentOn("TestAdapterUsingVSTest");

Task("Package")
	.IsDependentOn("PackageNuGet")
	.IsDependentOn("PackageVsix");

Task("Appveyor")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
