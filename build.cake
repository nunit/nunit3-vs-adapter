#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.1

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.5";
var modifier = "";

var isAppveyor = BuildSystem.IsRunningOnAppVeyor;
var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = version + modifier + dbgSuffix;

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

// Directories
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var NUNIT3_CONSOLE = PROJECT_DIR + "tools/NUnit.ConsoleRunner/tools/nunit3-console.exe";

// TODO: Consolidate in one directory
var ADAPTER_DIR = PROJECT_DIR + "src/NUnitTestAdapter/bin/" + configuration + "/";
var TEST_DIR = PROJECT_DIR + "src/NUnitTestAdapterTests/bin/" + configuration + "/";
var INSTALL_DIR = PROJECT_DIR + "src/NUnitTestAdapterInstall/bin/" + configuration + "/";

// Solution
var ADAPTER_SOLUTION = PROJECT_DIR + "NUnit3TestAdapter.sln";

// Test Assembly
var ADAPTER_TESTS = TEST_DIR + "NUnit.VisualStudio.TestAdapter.Tests.dll";

// Packages
var SRC_PACKAGE = PACKAGE_DIR + "NUnit3TestAdapter-" + version + modifier + "-src.zip";
var ZIP_PACKAGE = PACKAGE_DIR + "NUnit3TestAdapter-" + packageVersion + ".zip";

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(ADAPTER_DIR);
    CleanDirectory(TEST_DIR);
    CleanDirectory(INSTALL_DIR);
});


//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("InitializeBuild")
    .Does(() =>
{
    NuGetRestore(ADAPTER_SOLUTION);

	if (BuildSystem.IsRunningOnAppVeyor)
	{
		var tag = AppVeyor.Environment.Repository.Tag;

		if (tag.IsTag)
		{
			packageVersion = tag.Name;
		}
		else
		{
			var buildNumber = AppVeyor.Environment.Build.Number;
			packageVersion = version + "-CI-" + buildNumber + dbgSuffix;
			if (AppVeyor.Environment.PullRequest.IsPullRequest)
				packageVersion += "-PR-" + AppVeyor.Environment.PullRequest.Number;
			else
				packageVersion += "-" + AppVeyor.Environment.Repository.Branch;
		}

		AppVeyor.UpdateBuildVersion(packageVersion);
	}
});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("InitializeBuild")
    .Does(() =>
    {
		MSBuild(ADAPTER_SOLUTION, new MSBuildSettings()
			.SetConfiguration(configuration)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
			.SetVerbosity(Verbosity.Minimal)
			.SetNodeReuse(false)
		);
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
	{
        StartProcess(
			NUNIT3_CONSOLE,
			new ProcessSettings()
			{
				Arguments = ADAPTER_TESTS
			});
	});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("PackageSource")
  .Does(() =>
	{
		CreateDirectory(PACKAGE_DIR);
		RunGitCommand(string.Format("archive -o {0} HEAD", SRC_PACKAGE));
	});

Task("PackageZip")
	.IsDependentOn("Build")
	.Does(() =>
	{
		CreateDirectory(PACKAGE_DIR);

		var zipFiles = new FilePath[]
		{
			PROJECT_DIR + "README.md",
			ADAPTER_DIR + "NUnit3.TestAdapter.dll",
            ADAPTER_DIR + "nunit.engine.dll",
			ADAPTER_DIR + "nunit.engine.api.dll",
			ADAPTER_DIR + "Mono.Cecil.dll",
			ADAPTER_DIR + "Mono.Cecil.Pdb.dll",
			ADAPTER_DIR + "Mono.Cecil.Mdb.dll",
			ADAPTER_DIR + "Mono.Cecil.Rocks.dll"
		};

		Zip(ADAPTER_DIR, File(ZIP_PACKAGE), zipFiles);
	});

//////////////////////////////////////////////////////////////////////
// HELPER METHODS
//////////////////////////////////////////////////////////////////////

void RunGitCommand(string arguments)
{
	StartProcess("git", new ProcessSettings()
	{
		Arguments = arguments
	});
}

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .IsDependentOn("Clean")
	.IsDependentOn("Build");

Task("Package")
	.IsDependentOn("PackageSource")
	.IsDependentOn("PackageZip");

Task("Appveyor")
	.IsDependentOn("Build")
	.IsDependentOn("Test");
	//.IsDependentOn("Package");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
