using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class SinglePassingTestResultTests : AcceptanceTests
    {
        private static void AddTestsCs(IsolatedWorkspace workspace)
        {
            workspace.AddFile("Tests.cs", @"
                using NUnit.Framework;

                namespace Test
                {
                    public class Tests
                    {
                        [Test]
                        public void PassingTest()
                        {
                            Assert.Pass();
                        }
                    }
                }");
        }

        private static void AddTestsVb(IsolatedWorkspace workspace)
        {
            workspace.AddFile("Tests.vb", @"
                Imports NUnit.Framework

                Namespace Test
                    Public Class Tests

                        <Test>
                        Public Sub PassingTest()
                            Assert.Pass()
                        End Sub

                    End Class
                End Namespace");
        }

        [TestCaseSource(nameof(TargetFrameworks)), Platform("Win")]
        public static void Single_target_csproj(string targetFramework)
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFramework>{targetFramework}</TargetFramework>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");

            AddTestsCs(workspace);

            workspace.MsBuild(restore: true);

            workspace.VSTest($@"bin\Debug\{targetFramework}\Test.dll", VsTestFilter.NoFilter)
                .AssertSinglePassingTest();
        }

        [TestCaseSource(nameof(DotNetCliTargetFrameworks))]
        public static void Single_target_csproj_dotnet_CLI(string targetFramework)
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFramework>{targetFramework}</TargetFramework>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");

            AddTestsCs(workspace);

            workspace.DotNetTest().AssertSinglePassingTest();
        }

        [TestCaseSource(nameof(TargetFrameworks)), Platform("Win")]
        public static void Single_target_vbproj(string targetFramework)
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.vbproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFramework>{targetFramework}</TargetFramework>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");

            AddTestsVb(workspace);

            workspace.MsBuild(restore: true);

            workspace.VSTest($@"bin\Debug\{targetFramework}\Test.dll", VsTestFilter.NoFilter)
                .AssertSinglePassingTest();
        }

        [TestCaseSource(nameof(DotNetCliTargetFrameworks))]
        public static void Single_target_vbproj_dotnet_CLI(string targetFramework)
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.vbproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFramework>{targetFramework}</TargetFramework>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");

            AddTestsVb(workspace);

            workspace.DotNetTest().AssertSinglePassingTest();
        }

        [Test, Platform("Win")]
        public static void Multi_target_csproj()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFrameworks>{string.Join(";", TargetFrameworks)}</TargetFrameworks>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");

            AddTestsCs(workspace);

            workspace.MsBuild(restore: true);

            foreach (var targetFramework in TargetFrameworks)
            {
                workspace.VSTest($@"bin\Debug\{targetFramework}\Test.dll", VsTestFilter.NoFilter)
                    .AssertSinglePassingTest();
            }
        }

        [Test]
        public static void Multi_target_csproj_dotnet_CLI()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFrameworks>{string.Join(";", DotNetCliTargetFrameworks)}</TargetFrameworks>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");

            AddTestsCs(workspace);

            workspace.DotNetTest().AssertSinglePassingTest();
        }

        [Test, Platform("Win")]
        public static void Multi_target_vbproj()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.vbproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFrameworks>{string.Join(";", TargetFrameworks)}</TargetFrameworks>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");

            AddTestsVb(workspace);

            workspace.MsBuild(restore: true);

            foreach (var targetFramework in TargetFrameworks)
            {
                workspace.VSTest($@"bin\Debug\{targetFramework}\Test.dll", VsTestFilter.NoFilter)
                    .AssertSinglePassingTest();
            }
        }

        [Test]
        public static void Multi_target_vbproj_dotnet_CLI()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.vbproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFrameworks>{string.Join(";", DotNetCliTargetFrameworks)}</TargetFrameworks>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");

            AddTestsVb(workspace);

            workspace.DotNetTest().AssertSinglePassingTest();
        }

        [Test, Platform("Win")]
        public static void Legacy_csproj_with_PackageReference()
        {
            var workspace = CreateWorkspace();
            var nuvers = NuGetPackageVersion;
            workspace.AddProject("Test.csproj", $@"
                    <?xml version='1.0' encoding='utf-8'?>
                    <Project ToolsVersion='15.0' xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
                      <Import Project='$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props' Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
                      <PropertyGroup>
                        <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
                        <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
                        <ProjectGuid>{{25455E6C-F4D9-41B7-A227-1527D93799B0}}</ProjectGuid>
                        <OutputType>Library</OutputType>
                        <AppDesignerFolder>Properties</AppDesignerFolder>
                        <RootNamespace>Test</RootNamespace>
                        <AssemblyName>Test</AssemblyName>
                        <TargetFrameworkVersion>{LegacyProjectTargetFrameworkVersion}</TargetFrameworkVersion>
                        <FileAlignment>512</FileAlignment>
                        <Deterministic>true</Deterministic>
                      </PropertyGroup>
                      <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
                        <DebugSymbols>true</DebugSymbols>
                        <DebugType>full</DebugType>
                        <Optimize>false</Optimize>
                        <OutputPath>bin\Debug\</OutputPath>
                        <DefineConstants>DEBUG;TRACE</DefineConstants>
                        <ErrorReport>prompt</ErrorReport>
                        <WarningLevel>4</WarningLevel>
                      </PropertyGroup>
                      <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
                        <DebugType>pdbonly</DebugType>
                        <Optimize>true</Optimize>
                        <OutputPath>bin\Release\</OutputPath>
                        <DefineConstants>TRACE</DefineConstants>
                        <ErrorReport>prompt</ErrorReport>
                        <WarningLevel>4</WarningLevel>
                      </PropertyGroup>
                      <ItemGroup>
                        <Reference Include='System' />
                        <Reference Include='System.Core' />
                        <Reference Include='System.Xml.Linq' />
                        <Reference Include='System.Data.DataSetExtensions' />
                        <Reference Include='System.Data' />
                        <Reference Include='System.Xml' />
                      </ItemGroup>
                      <ItemGroup>
                        <Compile Include='Tests.cs' />
                      </ItemGroup>
                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk'>
                          <Version>15.9.0</Version>
                        </PackageReference>
                        <PackageReference Include='NUnit'>
                          <Version>3.11.0</Version>
                        </PackageReference>
                        <PackageReference Include='NUnit3TestAdapter'>
                          <Version>{nuvers}</Version>
                        </PackageReference>
                      </ItemGroup>
                      <Import Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets' />
                    </Project>");

            AddTestsCs(workspace);

            workspace.MsBuild(restore: true);

            var result = workspace.VSTest(@"bin\Debug\Test.dll", VsTestFilter.NoFilter);
            result.AssertSinglePassingTest();
        }

        [Test, Platform("Win")]
        public static void Legacy_vbproj_with_PackageReference()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.vbproj", $@"
                    <?xml version='1.0' encoding='utf-8'?>
                    <Project ToolsVersion='15.0' xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
                      <Import Project='$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props' Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
                      <PropertyGroup>
                        <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
                        <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
                        <ProjectGuid>{{C5C80224-B87B-4783-8CE1-45D64D940AAD}}</ProjectGuid>
                        <OutputType>Library</OutputType>
                        <RootNamespace>Test</RootNamespace>
                        <AssemblyName>Test</AssemblyName>
                        <FileAlignment>512</FileAlignment>
                        <MyType>Windows</MyType>
                        <TargetFrameworkVersion>{LegacyProjectTargetFrameworkVersion}</TargetFrameworkVersion>
                        <Deterministic>true</Deterministic>
                      </PropertyGroup>
                      <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
                        <DebugSymbols>true</DebugSymbols>
                        <DebugType>full</DebugType>
                        <DefineDebug>true</DefineDebug>
                        <DefineTrace>true</DefineTrace>
                        <OutputPath>bin\Debug\</OutputPath>
                        <DocumentationFile>Test.xml</DocumentationFile>
                        <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
                      </PropertyGroup>
                      <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
                        <DebugType>pdbonly</DebugType>
                        <DefineDebug>false</DefineDebug>
                        <DefineTrace>true</DefineTrace>
                        <Optimize>true</Optimize>
                        <OutputPath>bin\Release\</OutputPath>
                        <DocumentationFile>Test.xml</DocumentationFile>
                        <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
                      </PropertyGroup>
                      <PropertyGroup>
                        <OptionExplicit>On</OptionExplicit>
                      </PropertyGroup>
                      <PropertyGroup>
                        <OptionCompare>Binary</OptionCompare>
                      </PropertyGroup>
                      <PropertyGroup>
                        <OptionStrict>Off</OptionStrict>
                      </PropertyGroup>
                      <PropertyGroup>
                        <OptionInfer>On</OptionInfer>
                      </PropertyGroup>
                      <ItemGroup>
                        <Reference Include='System' />
                        <Reference Include='System.Data' />
                        <Reference Include='System.Xml' />
                        <Reference Include='System.Core' />
                        <Reference Include='System.Xml.Linq' />
                        <Reference Include='System.Data.DataSetExtensions' />
                      </ItemGroup>
                      <ItemGroup>
                        <Import Include='Microsoft.VisualBasic' />
                        <Import Include='System' />
                        <Import Include='System.Collections' />
                        <Import Include='System.Collections.Generic' />
                        <Import Include='System.Data' />
                        <Import Include='System.Diagnostics' />
                        <Import Include='System.Linq' />
                        <Import Include='System.Xml.Linq' />
                      </ItemGroup>
                      <ItemGroup>
                        <Compile Include='Tests.vb' />
                      </ItemGroup>
                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk'>
                          <Version>15.9.0</Version>
                        </PackageReference>
                        <PackageReference Include='NUnit'>
                          <Version>3.11.0</Version>
                        </PackageReference>
                        <PackageReference Include='NUnit3TestAdapter'>
                          <Version>{NuGetPackageVersion}</Version>
                        </PackageReference>
                      </ItemGroup>
                      <Import Project='$(MSBuildToolsPath)\Microsoft.VisualBasic.targets' />
                    </Project>");

            AddTestsVb(workspace);

            workspace.MsBuild(restore: true);

            workspace.VSTest(@"bin\Debug\Test.dll", VsTestFilter.NoFilter)
                .AssertSinglePassingTest();
        }

        private static void AddPackagesConfig(IsolatedWorkspace workspace)
        {
            workspace.AddFile("packages.config", $@"
                <?xml version='1.0' encoding='utf-8'?>
                <packages>
                    <package id='Microsoft.CodeCoverage' version='15.9.0' targetFramework='net462' />
                    <package id='Microsoft.NET.Test.Sdk' version='15.9.0' targetFramework='net462' />
                    <package id='NUnit' version='3.11.0' targetFramework='net462' />
                    <package id='NUnit3TestAdapter' version='{NuGetPackageVersion}' targetFramework='net462' />
                </packages>");
        }

        [Test, Platform("Win")]
        public static void Legacy_csproj_with_packages_config()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <?xml version='1.0' encoding='utf-8'?>
                    <Project ToolsVersion='15.0' xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
                      <Import Project='packages\NUnit3TestAdapter.{NuGetPackageVersion}\build\{LowestNetfxTarget}\NUnit3TestAdapter.props' Condition=""Exists('packages\NUnit3TestAdapter.{NuGetPackageVersion}\build\{LowestNetfxTarget}\NUnit3TestAdapter.props')"" />
                      <Import Project='packages\NUnit.3.11.0\build\NUnit.props' Condition=""Exists('packages\NUnit.3.11.0\build\NUnit.props')"" />
                      <Import Project='packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.props' Condition=""Exists('packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.props')"" />
                      <Import Project='packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.props' Condition=""Exists('packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.props')"" />
                      <Import Project='$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props' Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
                      <PropertyGroup>
                        <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
                        <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
                        <ProjectGuid>{{107E3C5F-61D7-439E-8B8D-8BF4C9506F06}}</ProjectGuid>
                        <OutputType>Library</OutputType>
                        <AppDesignerFolder>Properties</AppDesignerFolder>
                        <RootNamespace>Test</RootNamespace>
                        <AssemblyName>Test</AssemblyName>
                        <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
                        <FileAlignment>512</FileAlignment>
                        <Deterministic>true</Deterministic>
                        <NuGetPackageImportStamp>
                        </NuGetPackageImportStamp>
                      </PropertyGroup>
                      <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
                        <DebugSymbols>true</DebugSymbols>
                        <DebugType>full</DebugType>
                        <Optimize>false</Optimize>
                        <OutputPath>bin\Debug\</OutputPath>
                        <DefineConstants>DEBUG;TRACE</DefineConstants>
                        <ErrorReport>prompt</ErrorReport>
                        <WarningLevel>4</WarningLevel>
                      </PropertyGroup>
                      <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
                        <DebugType>pdbonly</DebugType>
                        <Optimize>true</Optimize>
                        <OutputPath>bin\Release\</OutputPath>
                        <DefineConstants>TRACE</DefineConstants>
                        <ErrorReport>prompt</ErrorReport>
                        <WarningLevel>4</WarningLevel>
                      </PropertyGroup>
                      <ItemGroup>
                        <Reference Include='Microsoft.VisualStudio.CodeCoverage.Shim, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL'>
                          <HintPath>packages\Microsoft.CodeCoverage.15.9.0\lib\net45\Microsoft.VisualStudio.CodeCoverage.Shim.dll</HintPath>
                        </Reference>
                        <Reference Include='nunit.framework, Version=3.11.0.0, Culture=neutral, PublicKeyToken=2638cd05610744eb, processorArchitecture=MSIL'>
                          <HintPath>packages\NUnit.3.11.0\lib\net45\nunit.framework.dll</HintPath>
                        </Reference>
                        <Reference Include='System' />
                        <Reference Include='System.Core' />
                        <Reference Include='System.Xml.Linq' />
                        <Reference Include='System.Data.DataSetExtensions' />
                        <Reference Include='Microsoft.CSharp' />
                        <Reference Include='System.Data' />
                        <Reference Include='System.Net.Http' />
                        <Reference Include='System.Xml' />
                      </ItemGroup>
                      <ItemGroup>
                        <Compile Include='Tests.cs' />
                        <None Include='packages.config' />
                      </ItemGroup>
                      <Import Project='$(MSBuildToolsPath)\Microsoft.CSharp.targets' />
                      <Target Name='EnsureNuGetPackageBuildImports' BeforeTargets='PrepareForBuild'>
                        <PropertyGroup>
                          <ErrorText>This project references NuGet package(s) that are missing on this computer. Use NuGet Package Restore to download them.  For more information, see https://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {{0}}.</ErrorText>
                        </PropertyGroup>
                        <Error Condition=""!Exists('packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.props')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.props'))"" />
                        <Error Condition=""!Exists('packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.targets')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.targets'))"" />
                        <Error Condition=""!Exists('packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.props')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.props'))"" />
                        <Error Condition=""!Exists('packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.targets')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.targets'))"" />
                        <Error Condition=""!Exists('packages\NUnit.3.11.0\build\NUnit.props')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\NUnit.3.11.0\build\NUnit.props'))"" />
                        <Error Condition=""!Exists('packages\NUnit3TestAdapter.{NuGetPackageVersion}\build\{LowestNetfxTarget}\NUnit3TestAdapter.props')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\NUnit3TestAdapter.{NuGetPackageVersion}\build\{LowestNetfxTarget}\NUnit3TestAdapter.props'))"" />
                      </Target>
                      <Import Project='packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.targets' Condition=""Exists('packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.targets')"" />
                      <Import Project='packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.targets' Condition=""Exists('packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.targets')"" />
                    </Project>");

            AddPackagesConfig(workspace);
            AddTestsCs(workspace);

            workspace.NuGetRestore(packagesDirectory: "packages");

            workspace.MsBuild();

            workspace.VSTest(@"bin\Debug\Test.dll", VsTestFilter.NoFilter)
                .AssertSinglePassingTest();
        }

        [Test, Platform("Win")]
        public static void Legacy_vbproj_with_packages_config()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.vbproj", $@"
                    <?xml version='1.0' encoding='utf-8'?>
                    <Project ToolsVersion='15.0' xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
                      <Import Project='packages\NUnit3TestAdapter.{NuGetPackageVersion}\build\{LowestNetfxTarget}\NUnit3TestAdapter.props' Condition=""Exists('packages\NUnit3TestAdapter.{NuGetPackageVersion}\build\{LowestNetfxTarget}\NUnit3TestAdapter.props')"" />
                      <Import Project='packages\NUnit.3.11.0\build\NUnit.props' Condition=""Exists('packages\NUnit.3.11.0\build\NUnit.props')"" />
                      <Import Project='packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.props' Condition=""Exists('packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.props')"" />
                      <Import Project='packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.props' Condition=""Exists('packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.props')"" />
                      <Import Project='$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props' Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
                      <PropertyGroup>
                        <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
                        <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
                        <ProjectGuid>{{70AE110C-1736-45F4-8941-5AA3435B7B49}}</ProjectGuid>
                        <OutputType>Library</OutputType>
                        <RootNamespace>Test</RootNamespace>
                        <AssemblyName>Test</AssemblyName>
                        <FileAlignment>512</FileAlignment>
                        <MyType>Windows</MyType>
                        <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
                        <Deterministic>true</Deterministic>
                        <NuGetPackageImportStamp>
                        </NuGetPackageImportStamp>
                      </PropertyGroup>
                      <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
                        <DebugSymbols>true</DebugSymbols>
                        <DebugType>full</DebugType>
                        <DefineDebug>true</DefineDebug>
                        <DefineTrace>true</DefineTrace>
                        <OutputPath>bin\Debug\</OutputPath>
                        <DocumentationFile>Test.xml</DocumentationFile>
                        <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
                      </PropertyGroup>
                      <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
                        <DebugType>pdbonly</DebugType>
                        <DefineDebug>false</DefineDebug>
                        <DefineTrace>true</DefineTrace>
                        <Optimize>true</Optimize>
                        <OutputPath>bin\Release\</OutputPath>
                        <DocumentationFile>Test.xml</DocumentationFile>
                        <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
                      </PropertyGroup>
                      <PropertyGroup>
                        <OptionExplicit>On</OptionExplicit>
                      </PropertyGroup>
                      <PropertyGroup>
                        <OptionCompare>Binary</OptionCompare>
                      </PropertyGroup>
                      <PropertyGroup>
                        <OptionStrict>Off</OptionStrict>
                      </PropertyGroup>
                      <PropertyGroup>
                        <OptionInfer>On</OptionInfer>
                      </PropertyGroup>
                      <ItemGroup>
                        <Reference Include='Microsoft.VisualStudio.CodeCoverage.Shim, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=MSIL'>
                          <HintPath>packages\Microsoft.CodeCoverage.15.9.0\lib\net45\Microsoft.VisualStudio.CodeCoverage.Shim.dll</HintPath>
                        </Reference>
                        <Reference Include='nunit.framework, Version=3.11.0.0, Culture=neutral, PublicKeyToken=2638cd05610744eb, processorArchitecture=MSIL'>
                          <HintPath>packages\NUnit.3.11.0\lib\net45\nunit.framework.dll</HintPath>
                        </Reference>
                        <Reference Include='System' />
                        <Reference Include='System.Data' />
                        <Reference Include='System.Xml' />
                        <Reference Include='System.Core' />
                        <Reference Include='System.Xml.Linq' />
                        <Reference Include='System.Data.DataSetExtensions' />
                        <Reference Include='System.Net.Http' />
                      </ItemGroup>
                      <ItemGroup>
                        <Import Include='Microsoft.VisualBasic' />
                        <Import Include='System' />
                        <Import Include='System.Collections' />
                        <Import Include='System.Collections.Generic' />
                        <Import Include='System.Data' />
                        <Import Include='System.Diagnostics' />
                        <Import Include='System.Linq' />
                        <Import Include='System.Xml.Linq' />
                        <Import Include='System.Threading.Tasks' />
                      </ItemGroup>
                      <ItemGroup>
                        <Compile Include='Tests.vb' />
                        <None Include='packages.config' />
                      </ItemGroup>
                      <Import Project='$(MSBuildToolsPath)\Microsoft.VisualBasic.targets' />
                      <Target Name='EnsureNuGetPackageBuildImports' BeforeTargets='PrepareForBuild'>
                        <PropertyGroup>
                          <ErrorText>This project references NuGet package(s) that are missing on this computer. Use NuGet Package Restore to download them.  For more information, see https://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {{0}}.</ErrorText>
                        </PropertyGroup>
                        <Error Condition=""!Exists('packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.props')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.props'))"" />
                        <Error Condition=""!Exists('packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.targets')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.targets'))"" />
                        <Error Condition=""!Exists('packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.props')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.props'))"" />
                        <Error Condition=""!Exists('packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.targets')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.targets'))"" />
                        <Error Condition=""!Exists('packages\NUnit.3.11.0\build\NUnit.props')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\NUnit.3.11.0\build\NUnit.props'))"" />
                        <Error Condition=""!Exists('packages\NUnit3TestAdapter.{NuGetPackageVersion}\build\{LowestNetfxTarget}\NUnit3TestAdapter.props')"" Text=""$([System.String]::Format('$(ErrorText)', 'packages\NUnit3TestAdapter.{NuGetPackageVersion}\build\{LowestNetfxTarget}\NUnit3TestAdapter.props'))"" />
                      </Target>
                      <Import Project='packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.targets' Condition=""Exists('packages\Microsoft.CodeCoverage.15.9.0\build\netstandard1.0\Microsoft.CodeCoverage.targets')"" />
                      <Import Project='packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.targets' Condition=""Exists('packages\Microsoft.NET.Test.Sdk.15.9.0\build\net45\Microsoft.Net.Test.Sdk.targets')"" />
                    </Project>");

            AddPackagesConfig(workspace);
            AddTestsVb(workspace);

            workspace.NuGetRestore(packagesDirectory: "packages");

            workspace.MsBuild();

            workspace.VSTest(@"bin\Debug\Test.dll", VsTestFilter.NoFilter)
                .AssertSinglePassingTest();
        }
    }
}
