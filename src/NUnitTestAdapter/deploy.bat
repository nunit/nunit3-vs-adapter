@echo off

setlocal

rem This runs in the directory contianing the output file
if /I "%1" == "Release" SET CONFIG=Release
set TARGETPATH=NUnit.VisualStudio.TestAdapter.dll
echo Deploying %TARGETPATH%

rem Deploy to experimental hive unless production is specified
set DEPLOYDIR=C:\Users\charlie\AppData\Local\Microsoft\VisualStudio\10.0Exp\Extensions\Extensions-10.0\Microsoft\Test Window\1.0\Extensions
if /I "%2" == "production" set DEPLOYDIR=C:\Users\charlie\AppData\Local\Microsoft\VisualStudio\10.0\Extensions\Microsoft\Test Window\1.0\Extensions
echo        to %DEPLOYDIR%

copy "%TARGETPATH%" "%DEPLOYDIR%" /Y
