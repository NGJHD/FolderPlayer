namespace AudioPlayer
{
    /// <summary>
    /// Everything the About box and the updater need to know about *this* app.
    /// Lifting the updater into another app means changing this file and nothing else.
    /// </summary>
    internal static class AboutInfo
    {
        public const string AppName = "Folder Player";
        public const string Author = "Darren Ng";

        //owner/name on github.com.
        public const string Repo = "NGJHD/FolderPlayer";

        //The release asset to download. The release must carry exactly one file matching this.
        public const string AssetSuffix = ".zip";

        //The file that proves an unpacked download really is this app, and the one the
        //apply script waits on and restarts.
        public const string ExeName = "AudioPlayer.exe";

        //Every staging folder this app leaves in %TEMP% starts with this, so abandoned
        //ones can be recognised and swept without touching anything else.
        public const string StagingPrefix = "folderplayer-update-";

        public static string RepoUrl
        {
            get { return "https://github.com/" + Repo; }
        }

        public static string LatestReleaseApiUrl
        {
            get { return "https://api.github.com/repos/" + Repo + "/releases/latest"; }
        }
    }
}
