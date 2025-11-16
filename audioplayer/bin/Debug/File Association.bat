@echo off
setlocal

:: Get the folder where this batch file is located
set "AppPath=%~dp0AudioPlayer.exe"

:: Remove trailing backslash for registry
set "AppPath=%AppPath:~0,-1%"

:: List of audio extensions to associate
set "EXTS=.mp3 .wav .flac .aac .ogg .m4a .wma"

for %%E in (%EXTS%) do (
    echo Associating %%E with AudioPlayer.exe...
    reg add "HKCU\Software\Classes\%%E" /ve /d "AudioPlayerApp" /f
    reg add "HKCU\Software\Classes\AudioPlayerApp\shell\open\command" /ve /d "\"%AppPath%\" \"%%1\"" /f
    echo.
)

echo Done! Audio files are now associated.
pause