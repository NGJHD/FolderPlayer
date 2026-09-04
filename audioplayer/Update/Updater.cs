using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioPlayer
{
    /// <summary>
    /// Thrown for anything the user should be told about in plain words. The message is shown
    /// verbatim in the About box, so it has to read like a sentence rather than an exception.
    /// </summary>
    internal sealed class UpdateException : Exception
    {
        public UpdateException(string message) : base(message) { }
    }

    internal sealed class DownloadProgress
    {
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
    }

    internal sealed class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public Version LatestVersion { get; set; }
        public string TagName { get; set; }
        public GitHubAsset Asset { get; set; }
        public string ReleaseUrl { get; set; }
    }

    /// <summary>
    /// Everything with a side effect: the GitHub call, the download, the unpack, the verify,
    /// and the script that swaps the files in once this process is gone.
    ///
    /// The app never overwrites itself - Windows holds a running exe open - so the last step
    /// hands the job to a detached cmd.exe and quits. Every failure before that point leaves
    /// the existing install untouched.
    /// </summary>
    internal static class Updater
    {
        static Updater()
        {
            //.NET 4.5 negotiates TLS 1.0 by default, which api.github.com has refused for
            //years. Without this line every request fails with a bare connection error.
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch (NotSupportedException)
            {
                //An older platform that cannot do TLS 1.2 at all. The request below then fails
                //with a network message, which is the honest thing to show.
            }
        }

        public static Version CurrentVersion
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return new Version(version.Major, version.Minor, version.Build);
            }
        }

        public static string InstallFolder
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        public static string ExePath
        {
            get { return Path.Combine(InstallFolder, AboutInfo.ExeName); }
        }

        /// <summary>
        /// True when running straight out of bin\Debug or bin\Release. Checking for updates
        /// still works there; installing one would overwrite the build output with a release,
        /// which is never what was meant.
        /// </summary>
        public static bool IsDevelopmentBuild()
        {
            string folder = InstallFolder;
            string leaf = Path.GetFileName(folder);
            string parentPath = Path.GetDirectoryName(folder);
            string parent = parentPath == null ? "" : Path.GetFileName(parentPath);

            bool underBin = string.Equals(parent, "bin", StringComparison.OrdinalIgnoreCase);
            bool configurationLeaf = string.Equals(leaf, "Debug", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(leaf, "Release", StringComparison.OrdinalIgnoreCase);

            return underBin && configurationLeaf;
        }

        /// <summary>
        /// Asked before the download starts, not after. An app unzipped under Program Files
        /// cannot replace itself, and finding that out at the end of a transfer is rude.
        /// </summary>
        public static bool IsInstallFolderWritable()
        {
            string probe = Path.Combine(InstallFolder, ".fp-write-probe-" + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                using (FileStream stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write))
                {
                    stream.WriteByte(0);
                }

                File.Delete(probe);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

/************************************************************************************************/
        //Check

        public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
        {
            string json;

            using (HttpClient client = CreateClient(TimeSpan.FromSeconds(30)))
            {
                HttpResponseMessage response;

                try
                {
                    response = await client.GetAsync(AboutInfo.LatestReleaseApiUrl, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new UpdateException("Could not reach GitHub. Check the network connection.");
                }

                using (response)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new UpdateException("No release has been published yet.");
                    }

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        //60 anonymous calls an hour per IP. A button cannot be pressed that
                        //fast, so this normally means a shared address.
                        throw new UpdateException("GitHub is rate limiting this connection. Try again in a little while.");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new UpdateException("GitHub answered with " + (int)response.StatusCode + " " + response.ReasonPhrase + ".");
                    }

                    json = await response.Content.ReadAsStringAsync();
                }
            }

            GitHubRelease release = ParseRelease(json);

            Version latest = VersionUtil.Parse(release.TagName);

            if (latest == null)
            {
                throw new UpdateException("The latest release is tagged \"" + release.TagName
                                          + "\", which is not a version this app can compare against.");
            }

            UpdateCheckResult result = new UpdateCheckResult
            {
                LatestVersion = latest,
                TagName = release.TagName,
                ReleaseUrl = release.HtmlUrl,
                UpdateAvailable = VersionUtil.IsNewer(latest, CurrentVersion),
            };

            if (result.UpdateAvailable)
            {
                result.Asset = VersionUtil.PickReleaseAsset(release.Assets);

                if (result.Asset == null)
                {
                    throw new UpdateException("Version " + Display(latest) + " is available, but its release has no "
                                              + AboutInfo.AssetSuffix + " to download.");
                }
            }

            return result;
        }

        private static GitHubRelease ParseRelease(string json)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                    GitHubRelease release = (GitHubRelease)serializer.ReadObject(stream);

                    if (release == null)
                    {
                        throw new UpdateException("The answer from GitHub could not be read.");
                    }

                    return release;
                }
            }
            catch (UpdateException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new UpdateException("The answer from GitHub could not be read.");
            }
        }

        private static HttpClient CreateClient(TimeSpan timeout)
        {
            HttpClient client = new HttpClient();
            client.Timeout = timeout;

            //The GitHub API rejects requests that arrive without a User-Agent.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FolderPlayer/" + Display(CurrentVersion));
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            return client;
        }

/************************************************************************************************/
        //Download

        /// <summary>
        /// A fresh staging folder under %TEMP%, carrying the prefix that makes it sweepable.
        /// </summary>
        public static string CreateStagingFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), AboutInfo.StagingPrefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        public static async Task<string> DownloadAsync(GitHubAsset asset, string stagingFolder,
                                                       IProgress<DownloadProgress> progress,
                                                       CancellationToken cancellationToken)
        {
            string zipPath = Path.Combine(stagingFolder, asset.Name);

            using (HttpClient client = CreateClient(TimeSpan.FromMinutes(30)))
            {
                HttpResponseMessage response;

                try
                {
                    response = await client.GetAsync(asset.BrowserDownloadUrl,
                                                     HttpCompletionOption.ResponseHeadersRead,
                                                     cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new UpdateException("Could not reach GitHub. Check the network connection.");
                }

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new UpdateException("The download failed with " + (int)response.StatusCode
                                                  + " " + response.ReasonPhrase + ".");
                    }

                    long total = response.Content.Headers.ContentLength ?? asset.Size;
                    long received = 0;

                    using (Stream source = await response.Content.ReadAsStreamAsync())
                    using (FileStream destination = new FileStream(zipPath, FileMode.Create, FileAccess.Write,
                                                                   FileShare.None, 81920, true))
                    {
                        byte[] buffer = new byte[81920];

                        while (true)
                        {
                            int read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                            if (read == 0)
                            {
                                break;
                            }

                            await destination.WriteAsync(buffer, 0, read, cancellationToken);
                            received += read;

                            if (progress != null)
                            {
                                progress.Report(new DownloadProgress { BytesReceived = received, TotalBytes = total });
                            }
                        }
                    }

                    if (total > 0 && received != total)
                    {
                        throw new UpdateException("The download ended early - " + VersionUtil.FormatBytes(received)
                                                  + " of " + VersionUtil.FormatBytes(total) + " arrived.");
                    }
                }
            }

            return zipPath;
        }

/************************************************************************************************/
        //Unpack and verify

        public static string Unpack(string zipPath, string stagingFolder)
        {
            string readyFolder = Path.Combine(stagingFolder, "ready");

            try
            {
                Directory.CreateDirectory(readyFolder);
                ZipFile.ExtractToDirectory(zipPath, readyFolder);
            }
            catch (Exception)
            {
                throw new UpdateException("The downloaded file could not be unpacked.");
            }

            return readyFolder;
        }

        /// <summary>
        /// Proves the unpacked folder is this app, at the version the tag claimed, and returns
        /// the folder to copy from. A version that disagrees is a hard stop - installing it
        /// anyway would offer the same update again on every launch from then on.
        /// </summary>
        public static string VerifyUnpacked(string readyFolder, Version expected)
        {
            string sourceFolder = readyFolder;

            if (!File.Exists(Path.Combine(sourceFolder, AboutInfo.ExeName)))
            {
                //Tolerate a zip that wraps everything in a single folder.
                string[] subFolders = Directory.GetDirectories(sourceFolder);

                if (subFolders.Length == 1 && File.Exists(Path.Combine(subFolders[0], AboutInfo.ExeName)))
                {
                    sourceFolder = subFolders[0];
                }
                else
                {
                    throw new UpdateException("The download does not contain " + AboutInfo.ExeName + ".");
                }
            }

            Version downloaded = ReadFileVersion(Path.Combine(sourceFolder, AboutInfo.ExeName));

            //Cannot read it: unknown, carry on. Read it and it disagrees: stop.
            if (downloaded != null && VersionUtil.Compare(downloaded, expected) != 0)
            {
                throw new UpdateException("The download is version " + Display(downloaded)
                                          + " but the release is tagged " + Display(expected)
                                          + ". Nothing has been changed.");
            }

            return sourceFolder;
        }

        private static Version ReadFileVersion(string exePath)
        {
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(exePath);
                return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart);
            }
            catch (Exception)
            {
                return null;
            }
        }

/************************************************************************************************/
        //Apply

        /// <summary>
        /// Writes the swap script, launches it, and returns. The caller closes the app straight
        /// after; the script waits for the exe to unlock, copies the new files over the install
        /// folder, and starts the app again.
        /// </summary>
        public static void LaunchApplyScript(string stagingFolder, string sourceFolder)
        {
            //The rd /s at the end of the script is pointed at a folder this process created,
            //under %TEMP%, carrying our prefix. Refuse to write that line for anything else.
            string stagingLeaf = Path.GetFileName(stagingFolder.TrimEnd(Path.DirectorySeparatorChar));

            if (!stagingLeaf.StartsWith(AboutInfo.StagingPrefix, StringComparison.Ordinal))
            {
                throw new UpdateException("Internal error: the staging folder is not one this app created.");
            }

            string scriptPath = Path.Combine(stagingFolder, "apply-update.cmd");

            string script = BuildApplyScript(sourceFolder, InstallFolder, ExePath,
                                             Path.Combine(InstallFolder, "update.log"), stagingFolder);

            //cmd.exe reads a batch file in the console OEM code page, so a user name with a
            //non-ASCII character in it turns every baked path into mojibake if this is written
            //as UTF-8.
            File.WriteAllText(scriptPath, script, OemEncoding());

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",

                //The script path stays its own argument - never shell out to an interpolated path.
                Arguments = "/c \"" + scriptPath + "\"",

                UseShellExecute = false,

                //No console window at all, and the child outlives this process either way.
                CreateNoWindow = true,

                //Anywhere but the install folder - a working directory is an open handle on it.
                WorkingDirectory = Path.GetTempPath(),
            };

            Process.Start(startInfo);
        }

        private static string BuildApplyScript(string sourceFolder, string targetFolder, string exePath,
                                               string logPath, string stagingFolder)
        {
            StringBuilder script = new StringBuilder();

            script.AppendLine("@echo off");
            script.AppendLine("title Updating " + AboutInfo.AppName);

            //Paths are baked in with set "VAR=value" rather than passed as arguments, so a path
            //with spaces in it has no quoting left to get wrong.
            script.AppendLine("set \"READY=" + EscapeForBatch(sourceFolder) + "\"");
            script.AppendLine("set \"TARGET=" + EscapeForBatch(targetFolder) + "\"");
            script.AppendLine("set \"EXE=" + EscapeForBatch(exePath) + "\"");
            script.AppendLine("set \"LOG=" + EscapeForBatch(logPath) + "\"");
            script.AppendLine("set \"STAGE=" + EscapeForBatch(stagingFolder) + "\"");
            script.AppendLine("set TRIES=0");
            script.AppendLine();
            script.AppendLine("echo [%DATE% %TIME%] waiting for " + AboutInfo.ExeName + " to unlock >>\"%LOG%\"");
            script.AppendLine();

            //Wait on the file lock, not on tasklist piped into find. That pipe hangs forever
            //when the parent has already died, and leaves a stray console window on the desktop.
            //Opening the exe for append and running a no-op writes zero bytes, leaves the file
            //size unchanged, and simply fails while the file is held - which is the precondition
            //that actually matters, since the exe stays locked a moment longer than the process.
            script.AppendLine(":waitloop");
            script.AppendLine("2>nul (>>\"%EXE%\" call ) && goto exited");
            script.AppendLine("set /a TRIES+=1");
            script.AppendLine("if %TRIES% GEQ 60 goto giveup");
            script.AppendLine("ping -n 2 127.0.0.1 >nul");
            script.AppendLine("goto waitloop");
            script.AppendLine();
            script.AppendLine(":giveup");
            script.AppendLine("echo [%DATE% %TIME%] still locked after 60 tries, nothing changed >>\"%LOG%\"");
            script.AppendLine("goto cleanup");
            script.AppendLine();
            script.AppendLine(":exited");

            //robocopy retries a locked file instead of giving up, and skips anything whose size
            //and timestamp already match. No /MIR: mirroring would delete MainConfig.xml and
            //whatever else the user keeps next to the exe.
            script.AppendLine("robocopy \"%READY%\" \"%TARGET%\" /E /R:3 /W:2 /NFL /NDL /NJH /NJS /NP >>\"%LOG%\" 2>&1");

            //robocopy exit codes 0-7 are success. A plain "if errorlevel 1" would report every
            //successful copy as a failure.
            script.AppendLine("if errorlevel 8 (");
            script.AppendLine("  echo [%DATE% %TIME%] robocopy failed, see above >>\"%LOG%\"");
            script.AppendLine("  goto cleanup");
            script.AppendLine(")");
            script.AppendLine("echo [%DATE% %TIME%] update applied >>\"%LOG%\"");
            script.AppendLine("start \"\" /D \"%TARGET%\" \"%EXE%\"");
            script.AppendLine();
            script.AppendLine(":cleanup");

            //This deletes the folder the running script is sitting in, so the .cmd itself
            //survives. The sweep on a later check takes it.
            script.AppendLine("rd /s /q \"%STAGE%\"");

            return script.ToString();
        }

        private static string EscapeForBatch(string path)
        {
            //Inside set "VAR=value" the only character still expanded is %.
            return path.Replace("%", "%%");
        }

        private static Encoding OemEncoding()
        {
            try
            {
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            }
            catch (Exception)
            {
                return Encoding.Default;
            }
        }

/************************************************************************************************/
        //Housekeeping

        /// <summary>
        /// A machine that loses power mid-update leaves a whole unpacked app in %TEMP%, and the
        /// apply script can never delete itself. Both get swept here, a day after they were last
        /// touched, every time the user checks.
        /// </summary>
        public static void SweepOldStagingFolders()
        {
            try
            {
                DateTime cutoff = DateTime.Now.AddDays(-1);

                foreach (string folder in Directory.GetDirectories(Path.GetTempPath(), AboutInfo.StagingPrefix + "*"))
                {
                    try
                    {
                        if (Directory.GetLastWriteTime(folder) < cutoff)
                        {
                            Directory.Delete(folder, true);
                        }
                    }
                    catch (Exception)
                    {
                        //In use, or gone already. Next time.
                    }
                }
            }
            catch (Exception)
            {
                //Housekeeping is never worth failing a check over.
            }
        }

        public static void DeleteStagingFolder(string stagingFolder)
        {
            if (string.IsNullOrEmpty(stagingFolder))
            {
                return;
            }

            string leaf = Path.GetFileName(stagingFolder.TrimEnd(Path.DirectorySeparatorChar));

            if (!leaf.StartsWith(AboutInfo.StagingPrefix, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                if (Directory.Exists(stagingFolder))
                {
                    Directory.Delete(stagingFolder, true);
                }
            }
            catch (Exception)
            {
                //Left for the sweep.
            }
        }

        public static string Display(Version version)
        {
            return version.ToString(3);
        }
    }
}
