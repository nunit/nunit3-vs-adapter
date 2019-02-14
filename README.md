# NUnit 3 VS Test Adapter #

The NUnit 3 Test Adapter runs NUnit 3.x tests in Visual Studio 2012 and newer.

This adapter works with NUnit 3.0 and higher only. Use the NUnit 2 Adapter to run NUnit 2.x tests.

## License ##

The NUnit 3 Test Adapter is Open Source software released under the [MIT license](https://nunit.org/nuget/nunit3-license.txt).

## Developing

Visual Studio 2017 is required to build the adapter.

Use `.\build -t test` at the command line to run complete tests.

To run and debug tests on .NET Framework, load `DisableAppDomain.runsettings`.

Visual Studio’s Test Explorer only allows you to run tests against the first target in the test project
[(upvote)](https://developercommunity.visualstudio.com/content/problem/150864/running-tests-in-a-csproj-with-multiple-targetfram.html).
That makes command line is the easiest way to run .NET Core tests for now. If you need to frequently debug into .NET Core tests,
you can temporarily switch the order of the `<TargetFrameworks>` in `NUnit.TestAdapter.Tests.csproj`.

The `mock-assembly` tests are not for direct running.

See https://github.com/nunit/docs/wiki/Packaging-the-V3-Adapter for more details.

## Debugger source-stepping

The NuGet package and the VSIX contain source-linked PDBs for the adapter.
If you’re in the middle of a debugging session and realize you’d like to be able to step into NUnit adapter code,
set breakpoints and watch variables, [follow these steps](https://github.com/nunit/docs/wiki/Adapter-Source-Stepping).

## Announcement
From version 3.9 the NUnit3TestAdapter will stop supporting Visual Studio 2012 RTM (!), note only RTM, the later updates will still be supported fully. If you're using VS 2012 and want to update NUnit3TestAdapter, please update your Visual Studio RTM to any of the subsequent updates (Update 1-5)
