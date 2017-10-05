# NUnit 3 VS Test Adapter #

The NUnit 3 Test Adapter runs NUnit 3.x tests in Visual Studio 2012 and newer.

This adapter works with NUnit 3.0 and higher only. Use the NUnit 2 Adapter to run NUnit 2.x tests.

## License ##

The NUnit 3 Test Adapter is Open Source software released under the [MIT license](http://www.nunit.org/nuget/nunit3-license.txt).

## Developing

You need Visual Studio 2017 for building the adapter

You will get some failing tests (6) in VS, that is intended.  

Use command line (Cake build):  

`build -t test` 

to get the right results, or exclude the tests from mock-assembly. 

For more details see https://github.com/nunit/docs/wiki/Packaging-the-V3-Adapter  


## Announcement
From version 3.9 the NUnit3Testadapter will stop supporting Visual Studio 2012 RTM (!), note only RTM, the later updates will still be supported fully. If you're using VS 2012 and want to update NUnit3TestAdapter , please update your Visual Studio RTM to any of the subsequent updates (Update 1-5)