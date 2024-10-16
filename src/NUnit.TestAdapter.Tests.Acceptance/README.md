# How to run the acceptance tests

## Running the tests by command line

```cmd
build -t acceptance
```

This will build and package the test adapter and run the acceptance tests.

## Running the tests in Visual Studio

You can also run the acceptance tests in Visual Studio. 
To do this, open the solution in Visual Studio and run the `NUnit.TestAdapter.Tests.Acceptance` Tests from the Test Explorer.

Note: Running the acceptance tests in Visual Studio require that the package are built and packaged on commandline before running the tests. 
This will create the `.acceptance` directory and add the package to the `package` directory.

