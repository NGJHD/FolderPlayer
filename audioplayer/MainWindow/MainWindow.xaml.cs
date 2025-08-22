using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        public Dictionary<string, SongClass> nowPlayingList = new Dictionary<string, SongClass>();
        private string nowPlayingSong = "";
        

        //History Related
        private List<Tuple<bool, string>> songHistoryList = new List<Tuple<bool, string>>(); //First bool is visibility, second string is filepath
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

            mainConfigXml.SelectSingleNode("Main/Volume").InnerText = volumeUserControl.GetLevel().ToString();// volumeBar.Value.ToString();
            mainConfigXml.SelectSingleNode("Main/Shuffle").InnerText = (shuffleState == STATE.ON ? "1" : "0");
            mainConfigXml.SelectSingleNode("Main/RepeatMode").InnerText = (repeatMode == REPEAT_MODE.OFF ? "0" : (repeatMode == REPEAT_MODE.PLAYLIST ? "1" : "2"));

            /*if (playlistListBox.SelectedIndex != -1)
            {
                mainConfigXml.SelectSingleNode("Main/LastKnown/PlaylistFilePath").InnerText = (playlistListBox.SelectedItem as PlayListClass).filepath;
            }
            else
            {
                mainConfigXml.SelectSingleNode("Main/LastKnown/PlaylistFilePath").InnerText = "";
            }*/

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

        public void KeyHandler(IntPtr wParam, IntPtr lParam)
        {
            int key = Marshal.ReadInt32(lParam);

            Hook.VK vk = (Hook.VK)key;

            if (key == 179)// Hook.VK.VK_F9)
            {
                playGrid_Click(null, null);
            }
            else if (key == 176)
            {
                nextGrid_Click(null, null);
            }
            else if (key == 177)
            {
                previousGrid_Click(null, null);
            }
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

            //volumeBar.Value = Convert.ToDouble(mainConfigXml.SelectSingleNode("Main/Volume").InnerText);
            volumeUserControl.SetLevel(Convert.ToInt32(mainConfigXml.SelectSingleNode("Main/Volume").InnerText));
            shuffleState = (Convert.ToInt32(mainConfigXml.SelectSingleNode("Main/Shuffle").InnerText) == 1 ? STATE.ON : STATE.OFF);
            int tempRepeat = Convert.ToInt32(mainConfigXml.SelectSingleNode("Main/RepeatMode").InnerText);
            repeatMode = (tempRepeat == 0 ? REPEAT_MODE.OFF : (tempRepeat == 1 ? REPEAT_MODE.PLAYLIST : REPEAT_MODE.SINGLE));

            //Hook the Keyboard
            Hook.CreateHook(KeyHandler);

            //Start listening to other instances of this wrapper
            /*System.Threading.Thread listenerThread = new System.Threading.Thread(udpListener);
            listenerThread.IsBackground = true;
            listenerThread.Start();*/

            string playSongFilePath = mainConfigXml.SelectSingleNode("Main/LastKnown/SongFilePath").InnerText;

            if (playSongFilePath.ToLower() == GlobalVariables.NowPlayingSingle.ToLower() || GlobalVariables.NowPlayingSingle == "")
            {                
                if (playSongFilePath != "")
                {
                    //Play the song
                    bool foundSongInPlaylist = playSong(playSongFilePath, true);

                    if (foundSongInPlaylist == false)
                    {
                        if (GlobalVariables.NowPlayingSingle != "")
                        {
                            justStarted_Play = true;
                        }

                        GlobalVariables.NowPlayingSingle = playSongFilePath;
                        playSong(new SongClass(playSongFilePath));                        
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
                        //writer.WriteElementString("PlaylistFilePath", "");
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
            if (shuffleState == STATE.ON)
            {
                shuffleState = STATE.OFF;
            }
            else
            {
                shuffleState = STATE.ON;
            }
        }

        private void repeatGrid_Click(object sender, EventArgs e)
        {
            if (repeatMode == REPEAT_MODE.OFF)
            {
                repeatMode = REPEAT_MODE.PLAYLIST;
            }
            else if (repeatMode == REPEAT_MODE.PLAYLIST)
            {
                repeatMode = REPEAT_MODE.SINGLE;
            }
            else
            {
                repeatMode = REPEAT_MODE.OFF;
            }
        }

        private void nextGrid_Click(object sender, EventArgs e)
        {
            if (playlistListBox.Items.Count == 0)
            {
                return;
            }

            if (repeatMode == REPEAT_MODE.SINGLE)
            {
                if (GlobalVariables.NowPlayingSingle == "")
                {
                    playSong(nowPlayingSong);
                }
                else
                {
                    playSong(new SongClass(GlobalVariables.NowPlayingSingle));
                }

                return;
            }

            if (nowPlayingList.Count == 0 && playlistListBox.Items.Count > 0)
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
                        playSong(songHistoryList[i].Item2);
                        return;
                    }
                }
            }

            if (shuffleState == STATE.ON)
            {
                for (int i = 0; i < nowPlayingList.Count; i++)
                {
                    (nowPlayingList.ElementAt(i).Value as SongClass).isPlaying = false;
                }

                nextSongIndex = new Random().Next(0, nowPlayingList.Count - 1);
            }
            else
            {
                for (int i = 0; i < nowPlayingList.Count; i++)
                {
                    SongClass songObj = nowPlayingList.ElementAt(i).Value as SongClass;

                    if (songObj.isPlaying == true)
                    {
                        songObj.isPlaying = false;

                        if (i == nowPlayingList.Count - 1)
                        {
                            if (repeatMode == REPEAT_MODE.OFF)
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

            playSong(nowPlayingList.ElementAt(nextSongIndex).Value);

            //Scroll into view on the listbox
            scrollNowPlayingSongIntoView();
        }

        private void previousGrid_Click(object sender, EventArgs e)
        {
            if (songHistoryList.Count == 0)
            {
                return;
            }

            //If it's less than 5 seconds, restart the song
            if (seekBar.Value > 3 && seekBar.Value < 5 || songHistoryList.Count == 1)
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
                    playSong(songHistoryList[Math.Max(i - 1, 0)].Item2);
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
            GlobalVariables.mainWindow.Title = "Folder Player";
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
                        playSong(new SongClass(GlobalVariables.NowPlayingSingle));                        
                    }
                    else
                    {
                        mediaPlayer.Play();
                    }
                }
                else if ((nowPlayingList.Count == 0 || nowPlayingSong == "") && playlistListBox.Items.Count > 0)
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

        private void scrollNowPlayingSongIntoView()
        {
            for (int i = 0; i < nowPlayingList.Count(); i++)
            {
                if (nowPlayingList.ElementAt(i).Value.filepath == nowPlayingSong)
                {
                    if (songlistListBox.Items.Count < i)
                    {
                        return;
                    }

                    songlistListBox.ScrollIntoView(songlistListBox.Items[i]);
                    break;
                }
            }
        }
/************************************************************************************************/
        private bool playSong(string songFilePath, bool scrollIntoView=false)
        {
            string playlistFilePath = System.IO.Path.GetDirectoryName(songFilePath);
            bool foundPlaylist = false;

            //Find the playlist
            for (int i = 0; i < playlistListBox.Items.Count; i++)
            {
                if ((playlistListBox.Items[i] as PlayListClass).filepath == playlistFilePath)
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
                SongClass songObj = songlistListBox.Items[i] as SongClass;

                if (songObj.filepath == songFilePath)
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

        public void playSong(SongClass songObj)
        {
            if (String.IsNullOrWhiteSpace(nowPlayingSong) == true)
            {
                songHistoryList.Add(new Tuple<bool, string>(true, songObj.filepath));
            }
            else
            {
                if (nowPlayingSong != songObj.filepath && doNotAddNext == false)
                {
                    for (int i = songHistoryList.Count - 1; i >= 0; i--)
                    {
                        if (songHistoryList[i].Item1 == false)
                        {
                            songHistoryList.RemoveAt(i);
                        }
                        else
                        {
                            songHistoryList.Add(new Tuple<bool, string>(true, songObj.filepath));
                            break;
                        }
                    }
                }
            }

            doNotAddNext = false;
            nowPlayingSong = songObj.filepath;
            try
            {
                trayIcon.Text = "Now Playing: " + System.IO.Path.GetFileName(nowPlayingSong);
            }
            catch (Exception)
            {
                trayIcon.Text = ("Now Playing: " + System.IO.Path.GetFileName(nowPlayingSong)).Substring(0, 63);
            }
            songObj.isPlaying = true;

            mediaPlayer.Source = new Uri(songObj.filepath, UriKind.Absolute);
            mediaPlayer.Play();

            playImage.Visibility = System.Windows.Visibility.Collapsed;
            pauseImage.Visibility = System.Windows.Visibility.Visible;

            if (GlobalVariables.NowPlayingSingle == "")
            {
                GlobalVariables.mainWindow.Title = "FOLDER PLAYER - " + System.IO.Path.GetFileNameWithoutExtension(songObj.filepath);
                nowPlayingSingleGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                GlobalVariables.mainWindow.Title = "FOLDER PLAYER - SINGLE - " + System.IO.Path.GetFileNameWithoutExtension(songObj.filepath);
                nowPlayingSingleTextBlock.Text = System.IO.Path.GetFileName(songObj.filepath);
                nowPlayingSingleGrid.Visibility = Visibility.Visible;
            }
        }
/************************************************************************************************/        
    }
}
