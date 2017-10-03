#tool nuget:?package=vswhere

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.8.0";
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

var ADAPTER_PROJECT = SRC_DIR + "NUnitTestAdapter/NUnit.TestAdapter.csproj";

var NET35_BIN_DIR = SRC_DIR + "NUnitTestAdapter/bin/" + configuration + "/net35/";

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

var TEST_NET35 = SRC_DIR + "NUnitTestAdapterTests/bin/" + configuration + "/net45/NUnit.VisualStudio.TestAdapter.Tests.exe";
var TEST_PROJECT = SRC_DIR + "NUnitTestAdapterTests/NUnit.TestAdapter.Tests.csproj";

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
    Information("Restoring NuGet Packages for the Adapter Solution");
	DotNetCoreRestore(ADAPTER_SOLUTION);

    Information("Restoring NuGet Packages for the VSIX project");
    NuGetRestore(PROJECT_DIR + "src/NUnit3TestAdapterInstall/NUnit3TestAdapterInstall.csproj",
                 new NuGetRestoreSettings {
                     PackagesDirectory = PROJECT_DIR + "packages"
                 });

    Information("Restoring NuGet Packages for the Demo .NET Core Project");
	DotNetCoreRestore(PROJECT_DIR + "demo/NUnit3CoreTestDemo/NUnit3CoreTestDemo.csproj");

    Information("Restoring NuGet Packages for the Demo .NET Project");
    NuGetRestore(PROJECT_DIR + "demo/NUnitTestDemo/NUnit3TestDemo.csproj",
                 new NuGetRestoreSettings {
                     PackagesDirectory = PROJECT_DIR + "demo/packages"
                 });
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

        MSBuild(PROJECT_DIR + "src/NUnitTestAdapterTests/NUnit.TestAdapter.Tests.csproj", settings);
        MSBuild(PROJECT_DIR + "src/NUnit3TestAdapterInstall/NUnit3TestAdapterInstall.csproj", settings);
		MSBuild(DEMO_SOLUTION, settings);
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("TestAdapter")
	.IsDependentOn("Build")
	.Does(() =>
	{
        int result = StartProcess(TEST_NET35);
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
        var VSTestCustomSettings = new VSTestSettings()
        {
            ArgumentCustomization = args => args.Append("/TestAdapterPath:" + NET35_BIN_DIR)
        };
		VSTest(TEST_NET35, VSTestCustomSettings);
	});

Task("TestDemo")
	.IsDependentOn("Build")
	.Does(() =>
	{
		try
		{
            var VSTestCustomSettings = new VSTestSettings()
            {
                ArgumentCustomization = args => args.Append("/TestAdapterPath:" + NET35_BIN_DIR)
            };
			VSTest(DEMO_TESTS, VSTestCustomSettings);
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
	.IsDependentOn("CreatePackageDir")
	.Does(() =>
	{
		CreateDirectory(PACKAGE_IMAGE_DIR);
		CleanDirectory(PACKAGE_IMAGE_DIR);

		CopyFileToDirectory("LICENSE.txt", PACKAGE_IMAGE_DIR);

        // dotnet publish doesn't work for .NET 3.5
		var net35Files = new FilePath[]
		{
			NET35_BIN_DIR + "NUnit3.TestAdapter.dll",
            NET35_BIN_DIR + "nunit.engine.dll",
			NET35_BIN_DIR + "nunit.engine.api.dll",
			NET35_BIN_DIR + "Mono.Cecil.dll",
			NET35_BIN_DIR + "Mono.Cecil.Pdb.dll",
			NET35_BIN_DIR + "Mono.Cecil.Mdb.dll",
			NET35_BIN_DIR + "Mono.Cecil.Rocks.dll"
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
	.IsDependentOn("TestAdapter")
	.IsDependentOn("TestAdapterNetCore")
	.IsDependentOn("TestAdapterUsingVSTest");

Task("Package")
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
