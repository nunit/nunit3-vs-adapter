#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.1

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.5.0";
var modifier = "";

var isAppveyor = BuildSystem.IsRunningOnAppVeyor;
var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = version + modifier + dbgSuffix;
var packageName = "NUnit3TestAdapter-" + packageVersion;

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
var PACKAGE_IMAGE_DIR = PACKAGE_DIR + packageName + "/";
var TOOLS_DIR = PROJECT_DIR + "tools/";

// TODO: Consolidate in one directory if possible
var ADAPTER_BIN_DIR = ADAPTER_DIR + "bin/" + configuration + "/";
var TEST_BIN_DIR = TEST_DIR + "bin/" + configuration + "/";
var INSTALL_BIN_DIR = INSTALL_DIR + "bin/" + configuration + "/";
var DEMO_BIN_DIR = DEMO_DIR + "NUnitTestDemo/bin/" + configuration + "/";

// Solutions
var ADAPTER_SOLUTION = PROJECT_DIR + "NUnit3TestAdapter.sln";
var DEMO_SOLUTION = DEMO_DIR + "NUnit3TestDemo.sln";

// Test Runners
var NUNIT3_CONSOLE = TOOLS_DIR + "NUnit.ConsoleRunner/tools/nunit3-console.exe";

// Test Assemblies
var ADAPTER_TESTS = TEST_BIN_DIR + "NUnit.VisualStudio.TestAdapter.Tests.dll";
var DEMO_TESTS = DEMO_BIN_DIR + "NUnit3TestDemo.dll";

// Packages
var SRC_PACKAGE = PACKAGE_DIR + "NUnit3TestAdapter-" + version + modifier + "-src.zip";
var ZIP_PACKAGE = PACKAGE_DIR + "NUnit3TestAdapter-" + packageVersion + ".zip";

// Custom settings for VSTest
var VSTestCustomSettings = new VSTestSettings()
{
	ArgumentCustomization = args => args.Append("/TestAdapterPath:" + ADAPTER_BIN_DIR)
};

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

Task("TestAdapterUsingConsole")
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

Task("TestAdapterUsingVSTest")
	.IsDependentOn("Build")
	.Does(() =>
	{
		VSTest(ADAPTER_TESTS, VSTestCustomSettings);
	});

Task("RunTestDemo")
	.IsDependentOn("Build")
	.Does(() =>
	{
		try
		{
			VSTest(DEMO_BIN_DIR + "NUnit3TestDemo.dll", VSTestCustomSettings);
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

Task("CreateWorkingImage")
	.IsDependentOn("Build")
	.IsDependentOn("CreatePackageDir")
	.Does(() =>
	{
		CreateDirectory(PACKAGE_IMAGE_DIR);
		CleanDirectory(PACKAGE_IMAGE_DIR);

		CopyFileToDirectory("LICENSE.txt", PACKAGE_IMAGE_DIR);

		var binFiles = new FilePath[]
		{
			ADAPTER_BIN_DIR + "NUnit3.TestAdapter.dll",
            ADAPTER_BIN_DIR + "nunit.engine.dll",
			ADAPTER_BIN_DIR + "nunit.engine.api.dll",
			ADAPTER_BIN_DIR + "Mono.Cecil.dll",
			ADAPTER_BIN_DIR + "Mono.Cecil.Pdb.dll",
			ADAPTER_BIN_DIR + "Mono.Cecil.Mdb.dll",
			ADAPTER_BIN_DIR + "Mono.Cecil.Rocks.dll"
		};

		var binDir = PACKAGE_IMAGE_DIR + "bin/";
		CreateDirectory(binDir);
		CopyFiles(binFiles, binDir);
	});

Task("PackageZip")
	.IsDependentOn("CreateWorkingImage")
	.Does(() =>
	{
		Zip(PACKAGE_IMAGE_DIR, File(ZIP_PACKAGE));
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
	.IsDependentOn("Build")
	.IsDependentOn("CreatePackageDir")
	.Does(() =>
	{
		CopyFile(
			INSTALL_BIN_DIR + "NUnit3TestAdapter.vsix", 
			PACKAGE_DIR + packageName + ".vsix");
	});

//////////////////////////////////////////////////////////////////////
// HELPER METHODS
//////////////////////////////////////////////////////////////////////

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

Task("Test")
	.IsDependentOn("TestAdapterUsingConsole")
	.IsDependentOn("TestAdapterUsingVSTest")
	.IsDependentOn("RunTestDemo");

Task("Package")
	.IsDependentOn("Build")
	.IsDependentOn("PackageZip")
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
