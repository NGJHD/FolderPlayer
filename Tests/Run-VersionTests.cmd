@echo off
rem Compiles VersionTests.cs against the pure half of the updater and runs it.
rem
rem VersionUtil has no network and no file system in it precisely so it can be exercised
rem like this - no test project, no NuGet, nothing to add to the solution. The rest of the
rem updater can only be tested against a real release; see "How to test the updater" in
rem the README.

setlocal

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo Could not find csc.exe for .NET Framework 4.
    exit /b 1
)

set "HERE=%~dp0"
set "SRC=%HERE%..\audioplayer\Update"
set "OUT=%TEMP%\folderplayer-versiontests.exe"

"%CSC%" /nologo /target:exe /out:"%OUT%" /r:System.Runtime.Serialization.dll ^
    "%SRC%\VersionUtil.cs" "%SRC%\AboutInfo.cs" "%SRC%\GitHubRelease.cs" "%HERE%VersionTests.cs"
if errorlevel 1 exit /b 1

"%OUT%"
set "RESULT=%ERRORLEVEL%"

del "%OUT%" >nul 2>&1
exit /b %RESULT%
