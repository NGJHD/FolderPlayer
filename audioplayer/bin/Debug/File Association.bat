@echo off
:: Registers FolderPlayer as a handler for audio files.
:: All work happens in Register-FileAssociations.ps1 next to this file.
:: Pass /remove to undo the registration.

setlocal

set "PS=%~dp0Register-FileAssociations.ps1"

if not exist "%PS%" (
    echo ERROR: Register-FileAssociations.ps1 not found next to this batch file.
    pause
    exit /b 1
)

if /i "%~1"=="/remove" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PS%" -Remove
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PS%" -OpenSettings
)

pause
