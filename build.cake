//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Versioning
var packageVersion = "3.0.0-ctp-7";

// Top level directories
//var baseDir = Directory(".");
//var srcDir = Directory("./src");
//var nugetDir = Directory("./nuget");
//var toolsDir = Directory("./tools");
//var libDir = Directory ("./lib");

// Output directories
var adapterOutput = Directory("./src/NUnitTestAdapter/bin") + Directory(configuration);
var testOutput = Directory("./src/NUnitTestAdapterTests/bin") + Directory(configuration);
var installOutput = Directory("./src/NunitTestAdapterInstall/bin") + Directory(configuration);
var mockOutput = Directory("./src/mock-assembly/bin") + Directory(configuration);

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(() =>
{
    Information("Building version {0} of Nunit.Xamarin.", packageVersion);
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] {
        adapterOutput, testOutput, installOutput, mockOutput});
});


Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./NUnit3TestAdapter.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
        // Use MSBuild
        MSBuild("./NUnit3TestAdapter.sln", new MSBuildSettings()
            .SetConfiguration(configuration)
            .SetPlatformTarget(PlatformTarget.MSIL)
//            .WithProperty("TreatWarningsAsErrors", "true")
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false)
        );
    }
    else
    {
        // Use XBuild
        XBuild("./NUnit3TestAdapter.sln", new XBuildSettings()
            .SetConfiguration(configuration)
            .WithTarget("AnyCPU")
//            .WithProperty("TreatWarningsAsErrors", "true")
            .SetVerbosity(Verbosity.Minimal)
        );
    }
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
