using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
/************************************************************************************************/
        //About overlay

        //Null until a check has found something newer. Its presence is what turns the primary
        //button from "Check for updates" into "Update".
        private UpdateCheckResult aboutPendingUpdate = null;

        private CancellationTokenSource aboutUpdateCancellation = null;

        //Set once the download is done and the files are being swapped in. The overlay refuses
        //to close while it is true, because there is nothing left to go back to.
        private bool aboutUpdateApplying = false;

        private void OnInfoButtonClick(object sender, RoutedEventArgs e)
        {
            //Read the version off the assembly rather than repeating it here, so this box and
            //AssemblyInfo.cs cannot drift apart.
            aboutVersionText.Text = "Version " + Updater.Display(Updater.CurrentVersion);
            aboutAuthorText.Text = "Made by " + AboutInfo.Author;
            aboutRepoLinkButton.Content = "github.com/" + AboutInfo.Repo;

            ResetAboutUpdateState();

            aboutOverlay.Visibility = Visibility.Visible;
            aboutPrimaryButton.Focus();
        }

        private void ResetAboutUpdateState()
        {
            aboutPendingUpdate = null;
            aboutUpdateApplying = false;
            aboutStatusText.Text = "";
            aboutProgressPanel.Visibility = Visibility.Collapsed;
            aboutProgressBar.Value = 0;
            aboutProgressText.Text = "";
            aboutCancelButton.Visibility = Visibility.Collapsed;
            aboutPrimaryButton.Content = "Check for updates";
            aboutPrimaryButton.IsEnabled = true;
        }

        private void CloseAboutOverlay()
        {
            if (aboutUpdateApplying)
            {
                return;
            }

            //Closing mid-download is a cancellation; the handler below tidies the staging folder.
            CancelAboutDownload();

            aboutOverlay.Visibility = Visibility.Collapsed;
        }

        private void CancelAboutDownload()
        {
            if (aboutUpdateCancellation != null)
            {
                try
                {
                    aboutUpdateCancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    //Already finished.
                }
            }
        }

        private void OnAboutCloseClick(object sender, RoutedEventArgs e)
        {
            CloseAboutOverlay();
        }

        private void OnAboutScrimMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Only a click on the dimmed area itself closes the box. The card has a background,
            //so a click anywhere on it never reaches here as the original source.
            if (ReferenceEquals(e.OriginalSource, aboutOverlay))
            {
                CloseAboutOverlay();
            }
        }

        private void OnAboutOverlayPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseAboutOverlay();
                e.Handled = true;
            }
        }

        private void OnAboutRepoLinkClick(object sender, RoutedEventArgs e)
        {
            OpenAboutLink(AboutInfo.RepoUrl);
        }

        /// <summary>
        /// The only outbound links this app opens are its own GitHub pages. The prefix is
        /// re-checked here rather than trusted from the caller, so this never becomes a
        /// general purpose "open any URL" hole.
        /// </summary>
        private void OpenAboutLink(string url)
        {
            if (url == null || !url.StartsWith(AboutInfo.RepoUrl, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                Process.Start(url);
            }
            catch (Exception)
            {
                aboutStatusText.Text = "Could not open a browser. The address is " + url;
            }
        }

/************************************************************************************************/
        //Check and install

        private async void OnAboutPrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            if (aboutPendingUpdate == null)
            {
                await CheckForUpdateAsync();
            }
            else
            {
                await InstallUpdateAsync(aboutPendingUpdate);
            }
        }

        private void OnAboutCancelDownloadClick(object sender, RoutedEventArgs e)
        {
            CancelAboutDownload();
        }

        private async Task CheckForUpdateAsync()
        {
            aboutPrimaryButton.IsEnabled = false;
            aboutStatusText.Text = "Checking github.com...";

            //A machine that lost power mid-update left a whole unpacked app in %TEMP%. This is
            //the moment to notice.
            await Task.Run((Action)Updater.SweepOldStagingFolders);

            aboutUpdateCancellation = new CancellationTokenSource();

            try
            {
                UpdateCheckResult result = await Updater.CheckAsync(aboutUpdateCancellation.Token);

                if (!result.UpdateAvailable)
                {
                    aboutStatusText.Text = "Version " + Updater.Display(Updater.CurrentVersion) + " is the latest.";
                    aboutPrimaryButton.Content = "Check for updates";
                }
                else
                {
                    aboutPendingUpdate = result;
                    aboutStatusText.Text = "Version " + Updater.Display(result.LatestVersion) + " is available ("
                                           + VersionUtil.FormatBytes(result.Asset.Size) + ").";
                    aboutPrimaryButton.Content = "Update to " + Updater.Display(result.LatestVersion);
                }
            }
            catch (OperationCanceledException)
            {
                aboutStatusText.Text = "";
            }
            catch (UpdateException exception)
            {
                aboutStatusText.Text = exception.Message;
            }
            catch (Exception exception)
            {
                aboutStatusText.Text = "The update check failed: " + exception.Message;
            }
            finally
            {
                DisposeAboutCancellation();
                aboutPrimaryButton.IsEnabled = true;
            }
        }

        private async Task InstallUpdateAsync(UpdateCheckResult update)
        {
            //Checking works fine out of bin\Debug; installing there would replace the build
            //output with a release, which is never what was meant.
            if (Updater.IsDevelopmentBuild())
            {
                aboutStatusText.Text = "This is a development build, so it cannot replace itself. "
                                       + "Download the release from GitHub instead.";
                return;
            }

            //Asked before the download rather than after it: an app unzipped somewhere it cannot
            //write is a refusal, not a wasted transfer.
            if (!Updater.IsInstallFolderWritable())
            {
                aboutStatusText.Text = "This folder cannot be written to, so the update cannot be "
                                       + "applied: " + Updater.InstallFolder;
                return;
            }

            string stagingFolder = null;

            aboutPrimaryButton.IsEnabled = false;
            aboutCancelButton.Visibility = Visibility.Visible;
            aboutCancelButton.IsEnabled = true;
            aboutProgressPanel.Visibility = Visibility.Visible;
            aboutProgressBar.Value = 0;
            aboutProgressText.Text = "";
            aboutStatusText.Text = "Downloading version " + Updater.Display(update.LatestVersion) + "...";

            aboutUpdateCancellation = new CancellationTokenSource();

            try
            {
                stagingFolder = Updater.CreateStagingFolder();

                //Constructed on the UI thread, so its callback arrives back on the UI thread.
                Progress<DownloadProgress> progress = new Progress<DownloadProgress>(ReportDownloadProgress);

                string zipPath = await Updater.DownloadAsync(update.Asset, stagingFolder, progress,
                                                             aboutUpdateCancellation.Token);

                //Past this point there is nothing left to abort, so a Cancel button would be lying.
                aboutUpdateApplying = true;
                aboutCancelButton.Visibility = Visibility.Collapsed;
                aboutProgressPanel.Visibility = Visibility.Collapsed;
                aboutStatusText.Text = "Unpacking...";

                string sourceFolder = await Task.Run(() =>
                {
                    string readyFolder = Updater.Unpack(zipPath, stagingFolder);
                    return Updater.VerifyUnpacked(readyFolder, update.LatestVersion);
                });

                aboutStatusText.Text = "Restarting to finish the update...";

                //The app cannot overwrite its own exe while it is running, so this hands the swap
                //to a detached script and quits. The script waits for the file to unlock, copies
                //the new files in, and starts the app again.
                Updater.LaunchApplyScript(stagingFolder, sourceFolder);

                //The script owns the staging folder now, and deletes it when it is done.
                stagingFolder = null;

                Close();
            }
            catch (OperationCanceledException)
            {
                aboutStatusText.Text = "Download cancelled. Version " + Updater.Display(update.LatestVersion)
                                       + " is still available (" + VersionUtil.FormatBytes(update.Asset.Size) + ").";
            }
            catch (UpdateException exception)
            {
                aboutStatusText.Text = exception.Message;
            }
            catch (Exception exception)
            {
                aboutStatusText.Text = "The update failed: " + exception.Message;
            }
            finally
            {
                DisposeAboutCancellation();

                //Only reached when something went wrong or the user cancelled - the successful
                //path has already closed the window.
                Updater.DeleteStagingFolder(stagingFolder);

                aboutUpdateApplying = false;
                aboutCancelButton.Visibility = Visibility.Collapsed;
                aboutProgressPanel.Visibility = Visibility.Collapsed;
                aboutPrimaryButton.IsEnabled = true;
            }
        }

        private void ReportDownloadProgress(DownloadProgress progress)
        {
            if (progress.TotalBytes > 0)
            {
                aboutProgressBar.Value = (progress.BytesReceived * 100.0) / progress.TotalBytes;

                //Bytes, not just a percentage: a bare bar cannot tell slow from stuck.
                aboutProgressText.Text = VersionUtil.FormatBytes(progress.BytesReceived) + " of "
                                         + VersionUtil.FormatBytes(progress.TotalBytes);
            }
            else
            {
                aboutProgressText.Text = VersionUtil.FormatBytes(progress.BytesReceived) + " downloaded";
            }
        }

        private void DisposeAboutCancellation()
        {
            if (aboutUpdateCancellation != null)
            {
                aboutUpdateCancellation.Dispose();
                aboutUpdateCancellation = null;
            }
        }
/************************************************************************************************/
    }
}
