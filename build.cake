#tool nuget:?package=vswhere

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.10.0";
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
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";

var ADAPTER_PROJECT = SRC_DIR + "NUnitTestAdapter/NUnit.TestAdapter.csproj";

var NET35_BIN_DIR = SRC_DIR + "NUnitTestAdapter/bin/" + configuration + "/net35/";

var BIN_DIRS = new [] {
    PROJECT_DIR + "src/empty-assembly/bin",
    PROJECT_DIR + "src/mock-assembly/bin",
    PROJECT_DIR + "src/NUnit3TestAdapterInstall/bin",
    PROJECT_DIR + "src/NUnit3TestAdapter/bin",
    PROJECT_DIR + "src/NUnit3TestAdapterTests/bin",
};

// Solution
var ADAPTER_SOLUTION = PROJECT_DIR + "NUnit3TestAdapter.sln";

// Test Assemblies
var TEST_NET35 = SRC_DIR + "NUnitTestAdapterTests/bin/" + configuration + "/net45/NUnit.VisualStudio.TestAdapter.Tests.exe";
var TEST_NETCOREAPP10 = SRC_DIR + "NUnitTestAdapterTests/bin/" + configuration + "/netcoreapp1.0/publish/NUnit.VisualStudio.TestAdapter.Tests.dll";

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
	DotNetCoreRestore(ADAPTER_SOLUTION);

    Information("Restoring NuGet Packages for the VSIX project");
    NuGetRestore(PROJECT_DIR + "src/NUnit3TestAdapterInstall/NUnit3TestAdapterInstall.csproj",
                 new NuGetRestoreSettings {
                     PackagesDirectory = PROJECT_DIR + "packages"
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
        Information("Configuration is:"+configuration);

        var settings = CreateSettings(msBuildPathX64);
        MSBuild(PROJECT_DIR + "src/NUnitTestAdapterTests/NUnit.TestAdapter.Tests.csproj", settings);
        MSBuild(PROJECT_DIR + "src/NUnit3TestAdapterInstall/NUnit3TestAdapterInstall.csproj", settings);

		Information("Publishing netcoreapp1.0 tests so that dependencies are present...");

        MSBuild(PROJECT_DIR + "src/NUnitTestAdapterTests/NUnit.TestAdapter.Tests.csproj", CreateSettings(msBuildPathX64)
			.WithTarget("Publish")
            .WithProperty("TargetFramework", "netcoreapp1.0")
            .WithProperty("NoBuild", "true") // https://github.com/dotnet/cli/issues/5331#issuecomment-338392972
			.WithRawArgument("/nologo"));

        MSBuildSettings CreateSettings(FilePath toolPath) => new MSBuildSettings
        {
            Configuration = configuration,
            ToolPath = toolPath,
            ToolVersion = MSBuildToolVersion.VS2017,
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["PackageVersion"] = packageVersion
            }
        };
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
		DotNetCoreExecute(TEST_NETCOREAPP10);
	});

Task("TestAdapterUsingVSTest")
	.IsDependentOn("Build")
	.Does(() =>
	{
		VSTest(TEST_NET35, new VSTestSettings()
            .WithRawArgument("/TestAdapterPath:" + NET35_BIN_DIR));
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
// HELPER METHODS
//////////////////////////////////////////////////////////////////////

public static T WithRawArgument<T>(this T settings, string rawArgument) where T : Cake.Core.Tooling.ToolSettings
{
    if (settings == null) throw new ArgumentNullException(nameof(settings));

    if (!string.IsNullOrEmpty(rawArgument))
    {
        var previousCustomizer = settings.ArgumentCustomization;
        if (previousCustomizer != null)
            settings.ArgumentCustomization = builder => previousCustomizer.Invoke(builder).Append(rawArgument);
        else
            settings.ArgumentCustomization = builder => builder.Append(rawArgument);
    }

    return settings;
}

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
