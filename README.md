# Folder Player
This personal project was started in 2016 due to a complete lack of folder player options for the Windows platform. It plays audio files by folders, hence there is no need to create a customized cumbersome playlist; the folder itself is the playlist. 

<img width="900" height="600" alt="image" src="https://github.com/user-attachments/assets/2e218d69-0072-4a69-a911-8e690d1151aa" />

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

## Releases

Tagged releases carry a prebuilt zip; the build output is not committed to the repository.

<details>
<summary>How to cut a release</summary>

1. Bump the version in `audioplayer\Properties\AssemblyInfo.cs` (`AssemblyVersion` and `AssemblyFileVersion`). The About box reads this value back off the assembly, so there is nothing else to update.
2. Build Release: `msbuild AudioPlayer.sln /p:Configuration=Release`
3. Zip just the three files a user needs - the `.pdb` is debug symbols and embeds your local source paths, so leave it out:
   ```powershell
   $files = 'AudioPlayer.exe', 'Register-FileAssociations.bat', 'Register-FileAssociations.ps1'
   $files | ForEach-Object { "audioplayer\bin\Release\$_" } |
       Compress-Archive -DestinationPath FolderPlayer-v2.1.0.zip -Force
   ```
4. Tag and push the commit:
   ```
   git tag -a v2.1.0 -m "Folder Player v2.1.0"
   git push origin v2.1.0
   ```
5. Create the release and attach the zip:
   ```
   gh release create v2.1.0 FolderPlayer-v2.1.0.zip --title "Folder Player v2.1.0" --notes "..."
   ```
   Or on github.com: **Releases → Draft a new release**, pick the tag, drag the zip into the assets box, publish.

</details>

## Licence

MIT — see [LICENSE](LICENSE).

`audioplayer/SingleInstance.cs` is Microsoft sample code (the WPF single-instance application helper) and carries its own copyright header; it is used here under the terms Microsoft published it under, and the MIT licence above covers the rest of this repository.
