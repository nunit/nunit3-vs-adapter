@echo off

setlocal

rem This runs in the output directory and copies the
rem key assemblies to the experimental hive
set TARGETPATH=NUnit.VisualStudio.TestAdapter.dll
echo Deploying %TARGETPATH%

rem Deploy to experimental hive unless production is specified
set DEPLOYDIR=C:\Users\charlie\AppData\Local\Microsoft\VisualStudio\11.0Exp\Extensions\Extensions-11.0\NUnitTestAdapter
if /I "%1" == "production" set DEPLOYDIR=C:\Users\charlie\AppData\Local\Microsoft\VisualStudio\11.0\Extensions\NUnitTestAdapter
echo        to %DEPLOYDIR%

md "%DEPLOYDIR%"
copy "%TARGETPATH%" "%DEPLOYDIR%" /Y
copy nunit.core.dll "%DEPLOYDIR%" /Y
copy nunit.core.interfaces.dll "%DEPLOYDIR%" /Y
copy nunit.util.dll "%DEPLOYDIR%" /Y
copy Microsoft.VisualStudio.TestPlatform.ObjectModel.dll "%DEPLOYDIR%" /Y
copy license.rtf "%DEPLOYDIR%" /Y
copy nunit_32x32.png "%DEPLOYDIR%" /Y
copy preview.png "%DEPLOYDIR%" /Y
copy extension.vsixmanifest "%DEPLOYDIR%" /Y