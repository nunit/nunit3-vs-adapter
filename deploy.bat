SETLOCAL

SET CONFIG=Debug
IF /I "%1" == "Release" SET CONFIG=Release
SET TARGETPATH=D:\Dev\NUnit\VisualStudioTestAdapter\NUnitTestAdapter\bin\%CONFIG%\NUnit.VisualStudio.TestAdapter.dll
echo Deploying %TARGETPATH%

IF /I "%2" <> "Exp" SET DEPLOYDIR=C:\Users\charlie\AppData\Local\Microsoft\VisualStudio\10.0\Extensions\10.0\Microsoft\Test Window\1.0\Extensions
IF /I "%2" == "Exp" SET DEPLOYDIR=C:\Users\charlie\AppData\Local\Microsoft\VisualStudio\10.0\Extensions\10.0\Microsoft\Test Window\1.0\Extensions
echo        to %DEPLOYDIR%

rem copy "%TARGETPATH% %DEPLOYDIR% /Y
