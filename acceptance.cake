#load lib.cake

using System.Xml.Linq;

const string acceptanceTestConfiguration = "Release";

Task("Acceptance")
    .IsDependentOn("Build")
    .IsDependentOn("PackageNuGet")
    .Description("Ensures that known project configurations can use the produced NuGet package to restore, build, and run tests.")
    .Does(() =>
    {
        DeleteDirectoryRobust(@"tests\Isolated package cache\nunit3testadapter");

        using (var tempDirectory = new TempDirectory())
        {

            BuildAndVerifySinglePassingTest("Simple", "net45", "netcoreapp1.0");
            BuildAndVerifySinglePassingTest("Referencing Mono.Cecil", "net45", "netcoreapp1.0");
            BuildAndVerifySinglePassingTest("Referencing Mono.Cecil 0.10.0", "net45", "netcoreapp1.0");
          
            void BuildAndVerifySinglePassingTest(string projectName, params string[] targetFrameworks)
            {
                var project = NewProjectFixture(Directory($@"tests\{projectName}"), acceptanceTestConfiguration, tempDirectory);
                project.Build(packageVersion);

                foreach (var targetFramework in targetFrameworks)
                {
                    VerifySinglePassingTest(project.Test(targetFramework));
                }
            }
        }
    });

void VerifySinglePassingTest(FilePath trxPath)
{
    var ns = (XNamespace)"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    var counters = XElement.Load(trxPath.FullPath)
        .Element(ns + "ResultSummary")
        .Element(ns + "Counters");

    if (counters.Attribute("total").Value != "1" || counters.Attribute("passed").Value != "1")
    {
        throw new Exception("Expected a single passing test result.");
    }
}
