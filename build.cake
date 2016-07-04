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
var ADAPTER_DIR = PROJECT_DIR + "src/NUnitTestAdapter/";
var TEST_DIR = PROJECT_DIR + "src/NUnitTestAdapterTests/";
var INSTALL_DIR = PROJECT_DIR + "src/NUnitTestAdapterInstall/";
var DEMO_DIR = PROJECT_DIR + "demo/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var TOOLS_DIR = PROJECT_DIR + "tools/";

// TODO: Consolidate in one directory if possible
var ADAPTER_BIN_DIR = ADAPTER_DIR + "bin/" + configuration + "/";
var TEST_BIN_DIR = TEST_DIR + "bin/" + configuration + "/";
var INSTALL_BIN_DIR = INSTALL_DIR + "bin/" + configuration + "/";
var DEMO_BIN_DIR = DEMO_DIR + "NUnitTestDemo/bin/" + configuration + "/";

// Solutions
var ADAPTER_SOLUTION = PROJECT_DIR + "NUnit3TestAdapter.sln";
var DEMO_SOLUTION = DEMO_DIR + "NUnit3TestDemo.sln";

// Test Runner
var NUNIT3_CONSOLE = TOOLS_DIR + "NUnit.ConsoleRunner/tools/nunit3-console.exe";

// Test Assemblies
var ADAPTER_TESTS = TEST_BIN_DIR + "NUnit.VisualStudio.TestAdapter.Tests.dll";
var DEMO_TESTS = DEMO_BIN_DIR + "NUnit3TestDemo.dll";

// Packages
var SRC_PACKAGE = PACKAGE_DIR + "NUnit3TestAdapter-" + version + modifier + "-src.zip";
var ZIP_PACKAGE = PACKAGE_DIR + "NUnit3TestAdapter-" + packageVersion + ".zip";

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(ADAPTER_BIN_DIR);
    CleanDirectory(TEST_BIN_DIR);
    CleanDirectory(INSTALL_BIN_DIR);
	CleanDirectory(DEMO_BIN_DIR);
});


//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("InitializeBuild")
    .Does(() =>
{
    NuGetRestore(ADAPTER_SOLUTION);
	NuGetRestore(DEMO_SOLUTION);

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
		BuildSolution(ADAPTER_SOLUTION, configuration);
		BuildSolution(DEMO_SOLUTION, configuration);
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
			ADAPTER_BIN_DIR + "NUnit3.TestAdapter.dll",
            ADAPTER_BIN_DIR + "nunit.engine.dll",
			ADAPTER_BIN_DIR + "nunit.engine.api.dll",
			ADAPTER_BIN_DIR + "Mono.Cecil.dll",
			ADAPTER_BIN_DIR + "Mono.Cecil.Pdb.dll",
			ADAPTER_BIN_DIR + "Mono.Cecil.Mdb.dll",
			ADAPTER_BIN_DIR + "Mono.Cecil.Rocks.dll"
		};

		Zip(ADAPTER_BIN_DIR, File(ZIP_PACKAGE), zipFiles);
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

void BuildSolution(string solutionPath, string configuration)
{
	MSBuild(solutionPath, new MSBuildSettings()
		.SetConfiguration(configuration)
        .SetMSBuildPlatform(MSBuildPlatform.x86)
		.SetVerbosity(Verbosity.Minimal)
		.SetNodeReuse(false)
	);
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
