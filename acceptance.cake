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
            var simple = NewProjectFixture(@"tests\Simple", acceptanceTestConfiguration, tempDirectory);
            simple.Build(packageVersion);
            VerifySinglePassingTest(simple.Test("net35"));
            VerifySinglePassingTest(simple.Test("netcoreapp1.0"));
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
