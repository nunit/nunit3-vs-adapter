# NUnit3 VS Adapter

## Building and testing

Use the Cake build script in the repo root (`build.cmd` or `build.ps1`). The targets:

| Command                | What it does                                                                |
|------------------------|-----------------------------------------------------------------------------|
| `build`                | Builds the solution (default target).                                       |
| `build -t test`        | Runs the unit tests on net462 and net8.0, with both VSTest and dotnet test. |
| `build -t package`     | Creates the NuGet package and zip in `package\`. It does not build first.   |
| `build -t acceptance`  | Builds, creates the NuGet package and runs the acceptance tests against it. |

Rules:

- Always run the acceptance tests with `build -t acceptance`. They test the NuGet package, so running the test project directly uses a stale or missing package.
- `build -t package` packs the existing binaries. Run `build` first, or the package contains old code.
- The package version comes from MinVer and the commit height, for example `6.4.0-alpha.0.25`. Each new commit raises it. The version is in the package file name in `package\`.

## Copying a package for local testing

After `build -t package`, copy the package to the local feed `c:\nuget` with:

```
.\copynp.cmd <version>
```

For example `.\copynp.cmd 6.4.0-alpha.0.25`. Give the user the version number when you build a package for them.
