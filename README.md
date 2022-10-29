# NUnit 3 VS Test Adapter #

The NUnit 3 Test Adapter runs NUnit 3.x tests in Visual Studio 2012 and newer.



You can download the latest release version ![](https://img.shields.io/github/release-date/nunit/nunit3-vs-adapter.svg?style=flat)

[![NuGet Version](https://img.shields.io/nuget/v/NUnit3TestAdapter.svg)](https://www.nuget.org/packages/NUnit3TestAdapter)  ![](https://img.shields.io/nuget/dt/NUnit3TestAdapter.svg?style=flat)

or the latest developer build version

[![MyGet version](https://img.shields.io/myget/nunit/vpre/NUnit3TestAdapter.svg?label=Myget%3A%20Latest%20pre-release&style=flat)](https://www.myget.org/feed/nunit/package/nuget/NUnit3TestAdapter)



##### Builds on master
[![Cake build](https://img.shields.io/azure-devops/build/nunit/9d7ec8eb-1a1a-4fff-a88f-43bcdceb5f33/12.svg)](https://nunit.visualstudio.com/NUnit/_build?definitionId=12&_a=completed)
[![VS Build](https://img.shields.io/azure-devops/build/nunit/9d7ec8eb-1a1a-4fff-a88f-43bcdceb5f33/4.svg)](https://nunit.visualstudio.com/NUnit/_build?definitionId=4&_a=completed)
[![Tests](https://img.shields.io/azure-devops/tests/nunit/nunit/4)](https://nunit.visualstudio.com/NUnit/_build?definitionId=4&_a=completed)
[![Coverage](https://img.shields.io/azure-devops/coverage/nunit/nunit/4.svg)](https://nunit.visualstudio.com/NUnit/_build?definitionId=4&_a=completed)



##### Support

Ask support questions [![Slack](https://img.shields.io/badge/chat-on%20Slack-brightgreen)](https://join.slack.com/t/nunit/shared_invite/zt-jz58jw68-Led8y3WH4n2a~Y5WjuOpKA)
or raise an issue [![](https://img.shields.io/github/issues/nunit/NUnit3-vs-Adapter.svg?style=flat)](https://github.com/nunit/nunit3-vs-adapter/issues)

## Documentation

The [NUnit3TestAdapter wiki](https://docs.nunit.org/articles/vs-test-adapter/Index.html) is the best place to start.

Also check the [release notes](https://docs.nunit.org/articles/vs-test-adapter/Adapter-Release-Notes.html).



## License ##


The NUnit 3 Test Adapter is Open Source software released under the [![](https://img.shields.io/github/license/nunit/nunit3-vs-adapter.svg?style=flat)](https://nunit.org/nuget/nunit3-license.txt).


## Developing

Visual Studio 2022 is the recommended version to build and test the adapter.

Use `.\build -t test` at the command line to build and run complete tests.

To create a package use `.\build -t package`

To run and debug tests on .NET Framework, load `DisableAppDomain.runsettings`.

From Visual Studio 2019 version 16.2 preview 4 it is possible to run tests against a selected target framework in the test project, so one can use this to run .NET Core tests.
An alternative approach is to make use of the command line. If you need to frequently debug into .NET Core tests, you can temporarily switch the order of the `<TargetFrameworks>` in `NUnit.TestAdapter.Tests.csproj`.

The `mock-assembly` tests are not for direct running.

See https://github.com/nunit/docs/wiki/Packaging-the-V3-Adapter for more details.

## Debugger source-stepping

The NuGet package and the VSIX contain source-linked PDBs for the adapter.
If you’re in the middle of a debugging session and realize you’d like to be able to step into NUnit adapter code,
set breakpoints and watch variables, [follow these steps](https://github.com/nunit/docs/wiki/Adapter-Source-Stepping).

## Notes

* This adapter works with NUnit 3.0 and higher only. Use the NUnit 2 Adapter to run NUnit 2.x tests.


## Announcements
* From version 3.9 the NUnit3TestAdapter will stop supporting Visual Studio 2012 RTM (!), note only RTM, the later updates will still be supported fully. If you're using VS 2012 and want to update NUnit3TestAdapter, please update your Visual Studio RTM to any of the subsequent updates (Update 1-5)
* From version 3.16 the NUnit3TestAdapter will stop supporting .net core 1 
* From version 4.0 the NUnit3TestAdapter will only be released as a nuget package, the VSIX is deprecated.
