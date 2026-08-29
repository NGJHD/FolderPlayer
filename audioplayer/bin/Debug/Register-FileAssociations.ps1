<#
.SYNOPSIS
    Registers FolderPlayer (AudioPlayer.exe) as a handler for audio file types.

.DESCRIPTION
    Writes a proper ProgID, an Applications\AudioPlayer.exe entry, per-extension
    OpenWithProgids entries, and an app Capabilities block so FolderPlayer shows
    up in Settings > Default apps.

    All writes are under HKEY_CURRENT_USER, so no administrator rights are needed
    and nothing is changed for other users on the machine.

    NOTE: Windows 10/11 protect the "UserChoice" default with a per-user hash, so
    no script can silently seize the default handler. This registers the app
    correctly; the final "make it the default" click happens in Settings or in the
    Open with dialog. Run with -OpenSettings to jump straight there.

.PARAMETER Remove
    Undo everything this script registers.

.PARAMETER OpenSettings
    Open Settings > Default apps at the FolderPlayer entry when done.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Register-FileAssociations.ps1
    powershell -ExecutionPolicy Bypass -File .\Register-FileAssociations.ps1 -Remove
#>
[CmdletBinding()]
param(
    [switch]$Remove,
    [switch]$OpenSettings
)

$ErrorActionPreference = 'Stop'

# --- Configuration ----------------------------------------------------------

$ExePath  = Join-Path $PSScriptRoot 'AudioPlayer.exe'
$ProgId   = 'FolderPlayer.AudioFile'
$AppName  = 'FolderPlayer'
$AppKey   = 'AudioPlayer.exe'          # name used under Software\Classes\Applications

# Formats WPF MediaElement (Windows Media Foundation) can play.
# The folder browser itself only lists .mp3/.wma/.m4a/.aac, but any of these
# will play when opened directly from Explorer.
$Extensions = @('.mp3', '.wma', '.m4a', '.aac', '.wav', '.flac')

$Classes  = 'HKCU:\Software\Classes'
$Caps     = "HKCU:\Software\$AppName\Capabilities"
$RegApps  = 'HKCU:\Software\RegisteredApplications'
$AppPaths = "HKCU:\Software\Microsoft\Windows\CurrentVersion\App Paths\$AppKey"

# --- Helpers ----------------------------------------------------------------

function Set-RegValue {
    param([string]$Path, [string]$Name, [string]$Value, [string]$Type = 'String')
    if (-not (Test-Path $Path)) { New-Item -Path $Path -Force | Out-Null }
    New-ItemProperty -Path $Path -Name $Name -Value $Value -PropertyType $Type -Force | Out-Null
}

function Set-RegDefault {
    param([string]$Path, [string]$Value)
    Set-RegValue -Path $Path -Name '(default)' -Value $Value
}

function Notify-Shell {
    $sig = @'
[DllImport("shell32.dll")]
public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
'@
    $shell = Add-Type -MemberDefinition $sig -Name 'ShellNotify' -Namespace 'FolderPlayer' -PassThru
    # SHCNE_ASSOCCHANGED (0x08000000), SHCNF_IDLIST (0x0000)
    $shell::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)
}

# --- Remove -----------------------------------------------------------------

if ($Remove) {
    Write-Host "Removing FolderPlayer file associations..." -ForegroundColor Cyan

    foreach ($ext in $Extensions) {
        $owp = "$Classes\$ext\OpenWithProgids"
        if (Test-Path $owp) {
            Remove-ItemProperty -Path $owp -Name $ProgId -ErrorAction SilentlyContinue
        }
        $extKey = "$Classes\$ext"
        if (Test-Path $extKey) {
            $current = (Get-ItemProperty $extKey -ErrorAction SilentlyContinue).'(default)'
            if ($current -eq $ProgId) {
                Set-RegDefault -Path $extKey -Value ''
            }
        }
        Write-Host "  cleared $ext"
    }

    Remove-Item "$Classes\$ProgId"            -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$Classes\Applications\$AppKey" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "HKCU:\Software\$AppName"     -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $AppPaths                     -Recurse -Force -ErrorAction SilentlyContinue
    Remove-ItemProperty -Path $RegApps -Name $AppName -ErrorAction SilentlyContinue

    Notify-Shell
    Write-Host "Done. Windows may still show FolderPlayer until you pick another default app." -ForegroundColor Green
    return
}

# --- Register ---------------------------------------------------------------

if (-not (Test-Path $ExePath)) {
    throw "AudioPlayer.exe not found next to this script (looked in $PSScriptRoot). Build the project first, or move this script beside the .exe."
}

$Command = '"{0}" "%1"' -f $ExePath
$Icon    = '{0},0' -f $ExePath

Write-Host "Registering $AppName" -ForegroundColor Cyan
Write-Host "  exe: $ExePath"
Write-Host ""

# 1. The ProgID that actually opens the file.
Set-RegDefault -Path "$Classes\$ProgId" -Value 'Audio File'
Set-RegValue   -Path "$Classes\$ProgId" -Name 'FriendlyTypeName' -Value 'Audio File'
Set-RegDefault -Path "$Classes\$ProgId\DefaultIcon" -Value $Icon
Set-RegDefault -Path "$Classes\$ProgId\shell\open\command" -Value $Command
Write-Host "  [ok] ProgID $ProgId"

# 2. Applications\AudioPlayer.exe - drives the "Open with" list, and is the
#    ProgID an existing .mp3 UserChoice may already point at.
$appRoot = "$Classes\Applications\$AppKey"
Set-RegValue   -Path $appRoot -Name 'FriendlyAppName' -Value $AppName
Set-RegDefault -Path "$appRoot\DefaultIcon" -Value $Icon
Set-RegDefault -Path "$appRoot\shell\open\command" -Value $Command
foreach ($ext in $Extensions) {
    Set-RegValue -Path "$appRoot\SupportedTypes" -Name $ext -Value ''
}
Write-Host "  [ok] Applications\$AppKey"

# 3. Per-extension: offer the ProgID, and claim the fallback default.
foreach ($ext in $Extensions) {
    Set-RegValue -Path "$Classes\$ext\OpenWithProgids" -Name $ProgId -Value ''
    Set-RegDefault -Path "$Classes\$ext" -Value $ProgId
    Write-Host "  [ok] $ext"
}

# 4. Capabilities - this is what puts FolderPlayer in Settings > Default apps.
Set-RegValue -Path $Caps -Name 'ApplicationName'        -Value $AppName
Set-RegValue -Path $Caps -Name 'ApplicationDescription' -Value 'Folder-based audio player'
Set-RegValue -Path $Caps -Name 'ApplicationIcon'        -Value $Icon
foreach ($ext in $Extensions) {
    Set-RegValue -Path "$Caps\FileAssociations" -Name $ext -Value $ProgId
}
Set-RegValue -Path $RegApps -Name $AppName -Value "Software\$AppName\Capabilities"
Write-Host "  [ok] Capabilities / RegisteredApplications"

# 5. App Paths - lets "AudioPlayer.exe" resolve by bare name (Run box, etc).
Set-RegDefault -Path $AppPaths -Value $ExePath
Set-RegValue   -Path $AppPaths -Name 'Path' -Value $PSScriptRoot
Write-Host "  [ok] App Paths"

Notify-Shell

Write-Host ""
Write-Host "Registered. Extensions: $($Extensions -join ' ')" -ForegroundColor Green
Write-Host ""
Write-Host "Windows will not let a script take over the default handler silently," -ForegroundColor Yellow
Write-Host "so finish it one of these two ways:" -ForegroundColor Yellow
Write-Host "  A) Settings > Apps > Default apps > FolderPlayer > set each type"
Write-Host "  B) Right-click an audio file > Open with > Choose another app >"
Write-Host "     FolderPlayer > 'Always use this app'"

if ($OpenSettings) {
    Start-Process "ms-settings:defaultapps"
}
