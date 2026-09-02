using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using KeyboardHook;
using System.Runtime.InteropServices;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
/************************************************************************************************/
        //Now Playing
        private List<FileInfo> tempListOfFiles = new List<FileInfo>();
        public Dictionary<string, Song> NowPlayingList = new Dictionary<string, Song>();
        private string nowPlayingSong = "";

        //Shared across every shuffle draw. A new Random() per call is seeded from the system
        //tick count, so calls close together in time would repeat the same "random" index.
        private static readonly Random shuffleRandom = new Random();

        //NotifyIcon.Text is limited to 63 characters plus the terminating null.
        private const int trayIconTextMaxLength = 63;

        //History Related
        private List<Tuple<bool, string>> songHistoryList = new List<Tuple<bool, string>>(); //First bool is visibility, second string is the file path
        private bool doNotAddNext = false;

        //Config
        private System.Xml.XmlDocument mainConfigXml = new System.Xml.XmlDocument();
        private string mainConfigXmlFileName = "MainConfig.xml";
        private bool justStarted_SeekBar = false;
        private bool justStarted_Play = false;
/************************************************************************************************/
        public MainWindow()
        {
            InitializeComponent();

            //Init Base Path
            string basePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
            mainConfigXmlFileName = basePath + mainConfigXmlFileName;

            //Seek Bar
            initSeekBar();

            //Create a Tray Icon
            initTrayIcon();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //Hide the tray icon
            trayIcon.Visible = false;

            //Close the Hook
            Hook.DestroyHook();
            
            //Close the player
            mediaPlayer.Close();

            mainConfigXml.SelectSingleNode("Main/Volume").InnerText = volumeUserControl.GetLevel().ToString();
            mainConfigXml.SelectSingleNode("Main/Shuffle").InnerText = (ShuffleState == ShuffleState.On ? "1" : "0");
            mainConfigXml.SelectSingleNode("Main/RepeatMode").InnerText = (RepeatMode == RepeatMode.Off ? "0" : (RepeatMode == RepeatMode.Playlist ? "1" : "2"));

            if (nowPlayingSong != "")
            {
                mainConfigXml.SelectSingleNode("Main/LastKnown/SongFilePath").InnerText = nowPlayingSong;
            }
            else if (GlobalVariables. NowPlayingSingle != "")
            {
                mainConfigXml.SelectSingleNode("Main/LastKnown/SongFilePath").InnerText = GlobalVariables.NowPlayingSingle;
            }
            else
            {
                mainConfigXml.SelectSingleNode("Main/LastKnown/SongFilePath").InnerText = "";
            }
            
            mainConfigXml.SelectSingleNode("Main/LastKnown/ElapsedTime").InnerText = seekBar.Value.ToString();

            mainConfigXml.Save(mainConfigXmlFileName);
        }

        //Called from inside the low-level keyboard hook, which runs for every keystroke on the
        //machine. Windows silently unhooks a callback that takes longer than LowLevelHooksTimeout
        //(~300 ms), so nothing here may touch the player directly - the work is queued onto the
        //dispatcher and the hook returns immediately.
        public void KeyHandler(IntPtr wParam, IntPtr lParam)
        {
            Hook.VK key = (Hook.VK)Marshal.ReadInt32(lParam);

            Action mediaAction;

            switch (key)
            {
                case Hook.VK.VK_MEDIA_PLAY_PAUSE:
                    mediaAction = () => playGrid_Click(null, null);
                    break;

                case Hook.VK.VK_MEDIA_NEXT_TRACK:
                    mediaAction = () => nextGrid_Click(null, null);
                    break;

                case Hook.VK.VK_MEDIA_PREV_TRACK:
                    mediaAction = () => previousGrid_Click(null, null);
                    break;

                default:
                    return;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Input, mediaAction);
        }

        private void mainWindow_ContentRendered(object sender, EventArgs e)
        {
            //Load from XML and put into playlist
            try
            {
                //Fill up the list box
                refreshPlayListListBox();
            }
            catch (Exception)
            {
                createMainConfig();
                refreshPlayListListBox();
            }

            volumeUserControl.SetLevel(Convert.ToInt32(mainConfigXml.SelectSingleNode("Main/Volume").InnerText));
            ShuffleState = (Convert.ToInt32(mainConfigXml.SelectSingleNode("Main/Shuffle").InnerText) == 1 ? ShuffleState.On : ShuffleState.Off);
            int tempRepeat = Convert.ToInt32(mainConfigXml.SelectSingleNode("Main/RepeatMode").InnerText);
            RepeatMode = (tempRepeat == 0 ? RepeatMode.Off : (tempRepeat == 1 ? RepeatMode.Playlist : RepeatMode.Single));

            //Hook the Keyboard
            Hook.CreateHook(KeyHandler);

            string playSongFilePath = mainConfigXml.SelectSingleNode("Main/LastKnown/SongFilePath").InnerText;

            if (playSongFilePath.ToLower() == GlobalVariables.NowPlayingSingle.ToLower() || GlobalVariables.NowPlayingSingle == "")
            {                
                if (playSongFilePath != "")
                {
                    //Play the song
                    bool foundSongInPlaylist = PlaySong(playSongFilePath, true);

                    if (foundSongInPlaylist == false)
                    {
                        if (GlobalVariables.NowPlayingSingle != "")
                        {
                            justStarted_Play = true;
                        }

                        GlobalVariables.NowPlayingSingle = playSongFilePath;
                        PlaySong(new Song(playSongFilePath));                        
                    }
                    
                    justStarted_SeekBar = true;
                }
            }
            else
            {
                mainConfigXml.SelectSingleNode("Main/LastKnown/SongFilePath").InnerText = "";
                mainConfigXml.SelectSingleNode("Main/LastKnown/ElapsedTime").InnerText = "";
                mainConfigXml.Save(mainConfigXmlFileName);
            }
        }

        private void createMainConfig()
        {
            using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(mainConfigXmlFileName))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Main");
                    writer.WriteElementString("Volume", "5");
                    writer.WriteElementString("Shuffle", "0");
                    writer.WriteElementString("RepeatMode", "1");
                writer.WriteStartElement("LastKnown");
                        writer.WriteElementString("SongFilePath", "");
                        writer.WriteElementString("ElapsedTime", "");
                    writer.WriteEndElement();
                    writer.WriteStartElement("PlayList");
                    writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            mainConfigXml.Load(mainConfigXmlFileName);
        }
/************************************************************************************************/
        private void shuffleGrid_Click(object sender, EventArgs e)
        {
            if (ShuffleState == ShuffleState.On)
            {
                ShuffleState = ShuffleState.Off;
            }
            else
            {
                ShuffleState = ShuffleState.On;
            }
        }

        private void repeatGrid_Click(object sender, EventArgs e)
        {
            if (RepeatMode == RepeatMode.Off)
            {
                RepeatMode = RepeatMode.Playlist;
            }
            else if (RepeatMode == RepeatMode.Playlist)
            {
                RepeatMode = RepeatMode.Single;
            }
            else
            {
                RepeatMode = RepeatMode.Off;
            }
        }

        private void nextGrid_Click(object sender, EventArgs e)
        {
            if (playlistListBox.Items.Count == 0)
            {
                return;
            }

            if (RepeatMode == RepeatMode.Single)
            {
                if (GlobalVariables.NowPlayingSingle == "")
                {
                    PlaySong(nowPlayingSong);
                }
                else
                {
                    PlaySong(new Song(GlobalVariables.NowPlayingSingle));
                }

                return;
            }

            if (NowPlayingList.Count == 0 && playlistListBox.Items.Count > 0)
            {
                playlistListBox_MouseDoubleClick(null, null);
                return;
            }

            int nextSongIndex = 0;

            //Check if there's a "next" song. If it is, it came from "previous button
            if (songHistoryList.Count() != 0 && songHistoryList.ElementAt(songHistoryList.Count() - 1).Item1 == false)
            {
                for (int i = 0; i < songHistoryList.Count(); i++)
                {
                    if (songHistoryList[i].Item1 == false)
                    {
                        doNotAddNext = true;
                        songHistoryList[i] = new Tuple<bool, string>(true, songHistoryList.ElementAt(i).Item2);
                        PlaySong(songHistoryList[i].Item2);
                        return;
                    }
                }
            }

            if (ShuffleState == ShuffleState.On)
            {
                for (int i = 0; i < NowPlayingList.Count; i++)
                {
                    (NowPlayingList.ElementAt(i).Value as Song).IsPlaying = false;
                }

                //Next(count) is exclusive on the upper bound, so this covers every index.
                nextSongIndex = shuffleRandom.Next(NowPlayingList.Count);
            }
            else
            {
                for (int i = 0; i < NowPlayingList.Count; i++)
                {
                    Song songObj = NowPlayingList.ElementAt(i).Value as Song;

                    if (songObj.IsPlaying == true)
                    {
                        songObj.IsPlaying = false;

                        if (i == NowPlayingList.Count - 1)
                        {
                            if (RepeatMode == RepeatMode.Off)
                            {
                                defaultState();
                                return;
                            }
                            else
                            {
                                nextSongIndex = 0;
                            }
                        }
                        else
                        {
                            nextSongIndex = i + 1;
                        }

                        break;
                    }
                }
            }

            PlaySong(NowPlayingList.ElementAt(nextSongIndex).Value);

            //Scroll into view on the listbox
            scrollNowPlayingSongIntoView();
        }

        private void previousGrid_Click(object sender, EventArgs e)
        {
            if (songHistoryList.Count == 0)
            {
                return;
            }

            //NO LONGER VALID: If it's less than 5 seconds, restart the song
            //NO LONGER VALID: if (seekBar.Value > 3 && seekBar.Value < 5 || songHistoryList.Count == 1)
            //2026-09-02, if it's more than 5 seconds, restart the song
            if (seekBar.Value > 5 || songHistoryList.Count == 1)
            {
                mediaPlayer.Stop();
                mediaPlayer.Play();
                return;
            }

            for (int i = songHistoryList.Count - 1; i >= 0; i--)
            {
                //The final true should be the one that's currently playing. 
                if (songHistoryList[i].Item1 == true)
                {
                    doNotAddNext = true;

                    if (i != 0)
                    {
                        songHistoryList[i] = new Tuple<bool, string>(false, songHistoryList.ElementAt(i).Item2);
                    }

                    //Play the song before the one that's currently playing
                    PlaySong(songHistoryList[Math.Max(i - 1, 0)].Item2);
                    break;
                }
            }
        }
/************************************************************************************************/
        private void defaultState()
        {
            //Stop the player
            mediaPlayer.Stop();
            seekBar.Value = 0;

            //Reset the play pause image
            playImage.Visibility = System.Windows.Visibility.Visible;
            pauseImage.Visibility = System.Windows.Visibility.Collapsed;

            //Reset parameters
            nowPlayingSong = "";
            trayIcon.Text = "Folder Player";
            GlobalVariables.MainWindow.Title = "Folder Player";
        }
        
        private void playGrid_Click(object sender, EventArgs e)
        {
            if (playImage.Visibility == System.Windows.Visibility.Visible)
            {
                //Just started            
                if (GlobalVariables.NowPlayingSingle != "")
                {
                    if (seekBar.Value == 0)
                    {
                        PlaySong(new Song(GlobalVariables.NowPlayingSingle));                        
                    }
                    else
                    {
                        mediaPlayer.Play();
                    }
                }
                else if ((NowPlayingList.Count == 0 || nowPlayingSong == "") && playlistListBox.Items.Count > 0)
                {
                    playlistListBox_MouseDoubleClick(null, null);
                }
                else
                {
                    mediaPlayer.Play();
                }

                playImage.Visibility = System.Windows.Visibility.Collapsed;
                pauseImage.Visibility = System.Windows.Visibility.Visible;
            }
            else //play button is visible. User wants to pause that's why hit the button.
            {
                mediaPlayer.Pause();
                playImage.Visibility = System.Windows.Visibility.Visible;
                pauseImage.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        //One-shot delayed callback on the UI thread. Replaces the pattern of spinning up a
        //throwaway OS thread purely to Thread.Sleep before dispatching back.
        private static void invokeAfter(TimeSpan delay, DispatcherPriority priority, Action action)
        {
            DispatcherTimer timer = new DispatcherTimer(priority);
            timer.Interval = delay;
            timer.Tick += (sender, e) =>
            {
                timer.Stop();
                action();
            };

            timer.Start();
        }

        private void scrollNowPlayingSongIntoView()
        {
            for (int i = 0; i < NowPlayingList.Count(); i++)
            {
                if (NowPlayingList.ElementAt(i).Value.FilePath == nowPlayingSong)
                {
                    //NowPlayingList can hold more entries than the visible list, e.g. right
                    //after switching playlists, so i is not necessarily a valid item index.
                    if (i >= songlistListBox.Items.Count)
                    {
                        return;
                    }

                    songlistListBox.ScrollIntoView(songlistListBox.Items[i]);
                    break;
                }
            }
        }
/************************************************************************************************/
        private bool PlaySong(string songFilePath, bool scrollIntoView=false)
        {
            string playlistFilePath = System.IO.Path.GetDirectoryName(songFilePath);
            bool foundPlaylist = false;

            //Find the playlist
            for (int i = 0; i < playlistListBox.Items.Count; i++)
            {
                if ((playlistListBox.Items[i] as Playlist).FilePath == playlistFilePath)
                {
                    playlistListBox.SelectedIndex = i;
                    foundPlaylist = true;
                    break;
                }
            }

            if (foundPlaylist == false)
            {
                return false;
            }

            //Find the song            
            for (int i = 0; i < songlistListBox.Items.Count; i++)
            {
                Song songObj = songlistListBox.Items[i] as Song;

                if (songObj.FilePath == songFilePath)
                {
                    songlistListBox.SelectedIndex = i;
                    songlistListBox_MouseDoubleClick(null, null);

                    if (scrollIntoView == true)
                    {
                        songlistListBox.ScrollIntoView(songlistListBox.SelectedItem);
                    }

                    return true;
                }
            }

            return false;
        }

        public void PlaySong(Song songObj)
        {
            if (String.IsNullOrWhiteSpace(nowPlayingSong) == true)
            {
                songHistoryList.Add(new Tuple<bool, string>(true, songObj.FilePath));
            }
            else
            {
                if (nowPlayingSong != songObj.FilePath && doNotAddNext == false)
                {
                    for (int i = songHistoryList.Count - 1; i >= 0; i--)
                    {
                        if (songHistoryList[i].Item1 == false)
                        {
                            songHistoryList.RemoveAt(i);
                        }
                        else
                        {
                            songHistoryList.Add(new Tuple<bool, string>(true, songObj.FilePath));
                            break;
                        }
                    }
                }
            }

            doNotAddNext = false;
            nowPlayingSong = songObj.FilePath;

            string trayIconText = "Now Playing: " + System.IO.Path.GetFileName(nowPlayingSong);
            trayIcon.Text = (trayIconText.Length > trayIconTextMaxLength
                                ? trayIconText.Substring(0, trayIconTextMaxLength)
                                : trayIconText);

            songObj.IsPlaying = true;

            mediaPlayer.Source = new Uri(songObj.FilePath, UriKind.Absolute);
            mediaPlayer.Play();

            playImage.Visibility = System.Windows.Visibility.Collapsed;
            pauseImage.Visibility = System.Windows.Visibility.Visible;

            if (GlobalVariables.NowPlayingSingle == "")
            {
                GlobalVariables.MainWindow.Title = "FOLDER PLAYER - " + System.IO.Path.GetFileNameWithoutExtension(songObj.FilePath);
                nowPlayingSingleGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                GlobalVariables.MainWindow.Title = "FOLDER PLAYER - SINGLE - " + System.IO.Path.GetFileNameWithoutExtension(songObj.FilePath);
                nowPlayingSingleTextBlock.Text = System.IO.Path.GetFileName(songObj.FilePath);
                nowPlayingSingleGrid.Visibility = Visibility.Visible;
            }
        }
/************************************************************************************************/        
    }
}
