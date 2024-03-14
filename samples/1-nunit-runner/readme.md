# NUnit runner

This sample project shows enabling the NUnit runner (name to be defined) powered by Microsoft.Testing.Platform.

When the runner is enabled an executable is produced and when run will use the new platform mode. Thanks to the compatibility layer (Microsoft.Testing.Extensions.VSTestBridge) all features and mode supported by VSTest remain available (dotnet test, VS/VS Code Test Explorer, AzDO VSTest task...) in addition to this new mode (calling the exe).

For example running `dotnet test` will still produce the regular output.

To go further, look at nunit-runner-dotnet-test
