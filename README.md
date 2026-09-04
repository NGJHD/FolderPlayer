# Folder Player
This personal project was started in 2016 due to a complete lack of folder player options for the Windows platform. It plays audio files by folders, hence there is no need to create a customized cumbersome playlist; the folder itself is the playlist. 

<img width="895" height="595" alt="project-music-player" src="https://github.com/user-attachments/assets/dc24fc55-df0e-4a19-8e69-aeb920f0316f" />

## Getting started

### Install

Grab the latest zip from [Releases](../../releases), unzip it anywhere, and run `AudioPlayer.exe`. Nothing is installed and nothing is written outside the folder you unzip into.

### Updating

The **i** button in the title bar opens an About box with a **Check for updates** button. It asks GitHub for the latest release, and if there is a newer one it downloads the zip, verifies it, and replaces the installed files with it - then restarts itself. Nothing checks on launch and nothing nags; the update only ever happens when you press the button.

If the folder you unzipped into is read-only, the update is refused before anything is downloaded, and the existing install is left alone.

# For developers
## Building

Open `AudioPlayer.sln` in Visual Studio 2015 or later and build (Debug or Release). The project targets .NET Framework 4.5 and has no NuGet or third-party dependencies, so a clean clone builds as-is.

From the command line:

```
msbuild AudioPlayer.sln /p:Configuration=Release
```

The output lands in `audioplayer\bin\Release\`.

## Running

Run `AudioPlayer.exe`. Drag a folder onto the left-hand list to add it as a playlist; the app remembers your folders, volume, shuffle and repeat settings in `MainConfig.xml`, which it creates next to the executable on first run.

Supported formats are the ones the WPF `MediaElement` can decode without extra codecs: `.mp3`, `.wma`, `.m4a`, `.aac`, `.wav` and `.flac`.

### File associations

To make Folder Player available as a handler for audio files, run `Register-FileAssociations.bat` from the output folder (it is copied next to the executable at build time). It writes only under `HKEY_CURRENT_USER`, so no administrator rights are needed.

Windows 8 and later protect the default-handler choice with a per-user hash, so no script can silently claim it. The script registers the app properly and then opens **Settings → Apps → Default apps**, where you make it the default in one click. Run `Register-FileAssociations.bat /remove` to undo the registration.

## Tests

`Tests\Run-VersionTests.cmd` compiles and runs the checks over `audioplayer\Update\VersionUtil.cs` - version parsing, comparison and release-asset picking. It uses the `csc.exe` that ships with .NET Framework 4, so there is nothing to install and nothing added to the solution.

The rest of the updater has side effects, and the only test that means anything is a real build updating itself off the real GitHub release. It takes about ten minutes.

<details>
<summary>How to test the updater</summary>

1. Claim to be older than the published release - set both versions in `AssemblyInfo.cs` to, say, `2.0.0.0`.
2. `msbuild AudioPlayer.sln /p:Configuration=Release`
3. Copy the output to a scratch folder, so nothing real is at risk: `robocopy audioplayer\bin\Release <scratch>\updtest /E`
4. Run `<scratch>\updtest\AudioPlayer.exe`, then **i** → **Check for updates** → **Update**.
5. Confirm all five:
   - the progress bar moves and shows bytes, not just a percentage
   - the app closes and comes back on its own
   - `(Get-Item <scratch>\updtest\AudioPlayer.exe).VersionInfo.ProductVersion` is now the release version
   - `<scratch>\updtest\update.log` ends with `update applied`
   - `%TEMP%` has no `folderplayer-update-*` folder left
6. Delete the scratch folder and restore the real version in `AssemblyInfo.cs`.

Worth exercising once each as well: already on the latest ("Version X is the latest.", no download offered), network off ("Could not reach GitHub."), cancel mid-download (back to the offer, staging folder deleted), a read-only install folder (refused **before** downloading, naming the path), and a dev run (check works, install says it is a development build).

</details>

## Releases

Tagged releases carry a prebuilt zip; the build output is not committed to the repository.

The in-app updater reads `/releases/latest`, so a release has to hold to three rules or the button cannot use it:

| | |
| --- | --- |
| **Tag** | `v<version>`, matching `AssemblyVersion` exactly - `v2.2.0` for `2.2.0.0`. The download is checked against the tag before anything is replaced, so a mismatch is a failed update rather than a silent downgrade. |
| **Asset** | Exactly one `.zip`, with `AudioPlayer.exe` at its root. |
| **Publish** | A real published release. `/releases/latest` skips drafts and pre-releases. |

<details>
<summary>How to cut a release</summary>

1. Bump the version in `audioplayer\Properties\AssemblyInfo.cs` (`AssemblyVersion` and `AssemblyFileVersion`). The About box and the updater both read this value back off the assembly, so there is nothing else to update.
2. Build Release: `msbuild AudioPlayer.sln /p:Configuration=Release`
3. Zip just the three files a user needs - the `.pdb` is debug symbols and embeds your local source paths, so leave it out:
   ```powershell
   $files = 'AudioPlayer.exe', 'Register-FileAssociations.bat', 'Register-FileAssociations.ps1'
   $files | ForEach-Object { "audioplayer\bin\Release\$_" } |
       Compress-Archive -DestinationPath FolderPlayer-v2.2.0.zip -Force
   ```
4. Tag and push the commit:
   ```
   git tag -a v2.2.0 -m "Folder Player v2.2.0"
   git push origin v2.2.0
   ```
5. Create the release and attach the zip:
   ```
   gh release create v2.2.0 FolderPlayer-v2.2.0.zip --title "Folder Player v2.2.0" --notes "..."
   ```
   Or on github.com: **Releases → Draft a new release**, pick the tag, drag the zip into the assets box, publish.

</details>

## Licence

MIT — see [LICENSE](LICENSE).

`audioplayer/SingleInstance.cs` is Microsoft sample code (the WPF single-instance application helper) and carries its own copyright header; it is used here under the terms Microsoft published it under, and the MIT licence above covers the rest of this repository.
