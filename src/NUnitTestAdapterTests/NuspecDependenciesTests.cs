// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

#nullable enable

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    /// <summary>
    /// Checks that the csproj of the framework projects are reflected correctly in the nuspec files.
    /// </summary>
    [TestFixture("NUnit3TestAdapter.nuspec", "NUnitTestAdapter/NUnit.TestAdapter.csproj")]
    internal sealed class NuspecDependenciesTests(string nuspecPath, string csProjPath)
    {
        private const string Root = "../../../../../";
        private const string NotSpecified = nameof(NotSpecified);

        private readonly string _nuspecPath = Path.GetFullPath($"{Root}/nuget/{nuspecPath}");
        private readonly string _csprojPath = Path.GetFullPath($"{Root}/src/{csProjPath}");
        private readonly string _propsPath = Path.GetFullPath($"{Root}/Directory.Packages.props");

        private static readonly HashSet<string> PackagesToIgnore =
        [
            "SourceLink.Create.CommandLine",  // Only used a as a development dependency in the adapter, so is only in csproj
            "nunit.engine",  // Is to be embedded - 3 files, so don't need to be in dependency list in nuspec, but must be in file list
            "TestCentric.Metadata",  // Only one file is embedded in the package, is in the file list
            "Microsoft.Testing.Platform.MSBuild",  // Must be in dependency list in nuspec, but dont need to be a package reference in csproj
            "Microsoft.Extensions.DependencyModel"  // Must be in dependency list in nuspec for version resolution (required by nunit.engine for net8.0), but dont need to be a package reference in csproj
        ];

        private Dictionary<string, List<PackageWithVersion>> _nuspecPackages;
        private Dictionary<string, List<PackageWithVersion>> _csprojPackages;
        private Dictionary<string, string> _csprojPackageVersions;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _nuspecPackages = NuspecReader.ExtractNuspecPackages(_nuspecPath, PackagesToIgnore);
            _csprojPackageVersions = PackageVersionReader.ExtractPackageVersions(_propsPath, PackagesToIgnore);
            _csprojPackages = CsprojReader.ExtractCsprojPackages(_csprojPath, _csprojPackageVersions, PackagesToIgnore);
        }

        [Test]
        public void AllPackagesInCsprojIsInNuspec()
        {
            VerifyDependencies.ComparePackages(_csprojPackages, _nuspecPackages);
        }

        [Test]
        public void AllPackagesInNuspecIsInCsproj()
        {
            VerifyDependencies.CheckNuspecPackages(_nuspecPackages, _csprojPackages);
        }

        private sealed class PackageWithVersion(string package, string version) : IEquatable<PackageWithVersion>
        {
            public string Package { get; } = package;
            public string Version { get; } = version;

            public override int GetHashCode()
            {
                return Package.GetHashCode() ^ Version.GetHashCode();
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as PackageWithVersion);
            }

            public bool Equals(PackageWithVersion? other)
            {
                return other is not null &&
                       Package == other.Package &&
                       Version == other.Version;
            }
        }

        private static class PackageVersionReader
        {
            // Function to extract packages from the .csproj file
            public static Dictionary<string, string> ExtractPackageVersions(
                string path, HashSet<string> packagesToIgnore)
            {
                Assert.That(path, Does.Exist, $"props file at {path} not found.");
                string xml = File.ReadAllText(path);

                var doc = XDocument.Parse(xml);
                var packageVersions = new Dictionary<string, string>();

                foreach (var itemGroup in doc.Descendants("ItemGroup"))
                {
                    var packageReferences = itemGroup.Descendants("PackageVersion");

                    foreach (var packageReference in packageReferences)
                    {
                        var package = packageReference.Attribute("Include")?.Value;
                        var version = packageReference.Attribute("Version")?.Value;

                        if (package is not null && version is not null && !packagesToIgnore.Contains(package))
                        {
                            if (packageVersions.TryGetValue(package, out string? previousVersion))
                            {
                                Assert.That(previousVersion, Is.EqualTo(version), $"Package {package} has multiple versions in the same file");
                            }

                            packageVersions[package] = version;
                        }
                    }
                }

                return packageVersions;
            }
        }

        private static class CsprojReader
        {
            // Function to extract packages from the .csproj file
            public static Dictionary<string, List<PackageWithVersion>> ExtractCsprojPackages(
                string path,
                Dictionary<string, string> packageVersions,
                HashSet<string> packagesToIgnore)
            {
                Assert.That(path, Does.Exist, $"Csproj file at {path} not found.");
                string xml = File.ReadAllText(path);

                var doc = XDocument.Parse(xml);
                var groupedPackages = new Dictionary<string, List<PackageWithVersion>>();

                foreach (var itemGroup in doc.Descendants("ItemGroup"))
                {
                    var condition = itemGroup.Attribute("Condition")?.Value;
                    string framework = NotSpecified;

                    if (!string.IsNullOrEmpty(condition) && condition!.Contains("TargetFrameworkIdentifier"))
                    {
                        var split = condition.Split(["=="], StringSplitOptions.RemoveEmptyEntries);
                        framework = split[1].Trim().Replace("'", string.Empty);
                    }

                    var packageReferences = itemGroup.Descendants("PackageReference")
                                                     .Select(pr => pr.Attribute("Include")?.Value)
                                                     .Where(include => !string.IsNullOrEmpty(include)) // Ensure it's non-null and non-empty
                                                     .Select(include => include!) // Use non-null assertion to ensure the result is a List<string>
                                                     .Where(include => !packagesToIgnore.Contains(include)) // Filter out ignored packages
                                                     .ToList();

                    var packageReferencesWithVersion = new List<PackageWithVersion>(packageReferences.Count);
                    foreach (var packageReference in packageReferences)
                    {
                        if (!packageVersions.TryGetValue(packageReference, out string? packageVersion))
                        {
                            Assert.Fail($"Package {packageReference} in .csproj does not have a version in Directory.Packages.props");
                        }
                        else
                        {
                            packageReferencesWithVersion.Add(new PackageWithVersion(packageReference, packageVersion));
                        }
                    }

                    if (itemGroup.Descendants("ProjectReference")
                                 .Select(pr => pr.Attribute("Include")?.Value)
                                 .Where(include => !string.IsNullOrEmpty(include)) // Ensure it's non-null and non-empty
                                 .Select(include => include!) // Use non-null assertion to ensure the result is a List<string>
                                 .Any(include => include.EndsWith("nunit.framework.csproj")))
                    {
                        packageReferencesWithVersion.Add(new PackageWithVersion("NUnit", "[$version$]"));
                    }

                    if (!groupedPackages.TryGetValue(framework, out List<PackageWithVersion>? versionedReferencesForFramework))
                    {
                        versionedReferencesForFramework = [];
                        groupedPackages[framework] = versionedReferencesForFramework;
                    }

                    versionedReferencesForFramework.AddRange(packageReferencesWithVersion);
                }

                return groupedPackages;
            }
        }

        private static class NuspecReader
        {
            public static Dictionary<string, List<PackageWithVersion>> ExtractNuspecPackages(
                string path, HashSet<string> packagesToIgnore)
            {
                Assert.That(path, Does.Exist, $"Nuspec file at {path} not found.");
                string xml = File.ReadAllText(path);

                var doc = XDocument.Parse(xml);
                XNamespace ns = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd";

                // Dictionary to hold dependencies grouped by targetFramework
                var groupedDependencies = new Dictionary<string, List<PackageWithVersion>>();

                // Find the dependencies section
                var dependenciesSection = doc.Descendants(ns + "dependencies").FirstOrDefault();
                if (dependenciesSection is not null)
                {
                    // Iterate over all group elements within the dependencies block
                    foreach (var group in dependenciesSection.Elements(ns + "group"))
                    {
                        // Get the targetFramework attribute
                        var framework = group.Attribute("targetFramework")?.Value ?? NotSpecified;

                        // Find all dependency elements in the current group
                        var dependencies = group.Elements(ns + "dependency");

                        var versionedDependencies = new List<PackageWithVersion>();
                        foreach (var dependency in dependencies)
                        {
                            var id = dependency.Attribute("id")?.Value;
                            var version = dependency.Attribute("version")?.Value;

                            if (id is not null && version is not null && !packagesToIgnore.Contains(id))
                            {
                                versionedDependencies.Add(new PackageWithVersion(id, version));
                            }
                        }

                        // Add dependencies to the respective framework group
                        if (!groupedDependencies.TryGetValue(framework, out List<PackageWithVersion>? versionedDependenciesForFramework))
                        {
                            versionedDependenciesForFramework = new List<PackageWithVersion>();
                            groupedDependencies[framework] = versionedDependenciesForFramework;
                        }

                        versionedDependenciesForFramework.AddRange(versionedDependencies);
                    }
                }

                return groupedDependencies;
            }
        }

        private sealed class VerifyDependencies
        {
            public static void ComparePackages(
                Dictionary<string, List<PackageWithVersion>> csprojPackages,
                Dictionary<string, List<PackageWithVersion>> nuspecPackages)
            {
                // Iterate through the frameworks in the csprojPackages dictionary
                foreach (var csprojFramework in csprojPackages.Keys)
                {
                    if (csprojFramework == NotSpecified)
                    {
                        TestContext.Out.WriteLine("Checking for packages that should be in all frameworks in the .nuspec file");
                        // Check if the packages from the csproj are present in all nuspec framework
                        Assert.Multiple(() =>
                        {
                            foreach (var framework in nuspecPackages.Keys)
                            {
                                MatchForSingleFramework(framework, csprojPackages[csprojFramework], nuspecPackages[framework]);
                            }
                        });
                    }
                    else
                    {
                        TestContext.Out.WriteLine($"Checking for packages that should be in corresponding '{csprojFramework}' in the .nuspec file");
                        // Handle specific framework case
                        var matchingNuspecFramework = nuspecPackages.Keys.FirstOrDefault(nuspecFramework =>
                            (csprojFramework == ".NETFramework" && nuspecFramework.StartsWith("net") &&
                             int.TryParse(nuspecFramework.Substring(3), out var version) && version >= 462) ||
                            (csprojFramework != ".NETFramework" && nuspecFramework == csprojFramework));

                        // Assert that the matching framework was found
                        Assert.That(matchingNuspecFramework, Is.Not.Null, $"Framework '{csprojFramework}' is in .csproj but not in .nuspec.");

                        // Find packages in csproj that are missing in nuspec
                        MatchForSingleFramework(matchingNuspecFramework, csprojPackages[csprojFramework], nuspecPackages[matchingNuspecFramework]);
                    }
                }
            }

            private static void MatchForSingleFramework(string framework, List<PackageWithVersion> csprojPackages,
                List<PackageWithVersion> nuspecPackages)
            {
                List<string> csProjPackagesForFramework = csprojPackages.Select(x => x.Package).ToList();
                List<string> nuspecPackagesForFramework = nuspecPackages.Select(x => x.Package).ToList();
                var missingPackages = csProjPackagesForFramework.Except(nuspecPackagesForFramework).ToList();

                Assert.Multiple(() =>
                {
                    // Assert that there are no missing packages in any nuspec framework
                    Assert.That(missingPackages, Is.Empty,
                        $"Missing packages in framework '{framework}' in .nuspec: {string.Join(", ", missingPackages)}");

                    foreach (var pair in csprojPackages)
                    {
                        var nuspecVersion = nuspecPackages.First(x => x.Package == pair.Package).Version;
                        Assert.That(nuspecVersion, Is.EqualTo(pair.Version), $"Package {pair.Package} in .csproj should have version '{pair.Version}' in .nuspec");
                    }
                });
            }

            public static void CheckNuspecPackages(
                Dictionary<string, List<PackageWithVersion>> nuspecPackages,
                Dictionary<string, List<PackageWithVersion>> csprojPackages)
            {
                // Extract all packages from csproj
                var allCsprojPackages = csprojPackages.Values.SelectMany(x => x).Select(x => x.Package).ToList();

                // Extract all packages from nuspec
                var allNuspecPackages = nuspecPackages.Values.SelectMany(x => x).Select(x => x.Package).ToList();

                // Find packages in nuspec that are missing in csproj
                var missingPackages = allNuspecPackages.Except(allCsprojPackages).ToList();
                Assert.That(missingPackages, Is.Empty, $"Packages in .nuspec that are not in .csproj and should be deleted from nuspec: {string.Join(", ", missingPackages)}");
            }
        }
    }
}
