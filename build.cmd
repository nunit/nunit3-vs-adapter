@echo off
powershell -executionPolicy bypass ./build.ps1 %CAKE_ARGS% %*
