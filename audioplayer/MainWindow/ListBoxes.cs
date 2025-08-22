using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
/************************************************************************************************/
        private static string[] audioExtensions = { ".MP3", ".WMA", ".M4A", ".AAC" };
/************************************************************************************************/
        private void playlistListBox_Drop(object sender, DragEventArgs e)
        {
            //Loop through the list of files dropped
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string filepath in files)
            {
                //Check if the path is added before
                bool found = false;
                for (int i = 0; i < playlistListBox.Items.Count; i++)
                {
                    if ((playlistListBox.Items[i] as PlayListClass).filepath == filepath)
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    //Check if it's a directory
                    if ((File.GetAttributes(filepath) & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        //Add to the playlist listbox
                        playlistListBox.Items.Add(new PlayListClass(filepath));

                        //Add filepath to the xml database
                        System.Xml.XmlNode filePathNode = mainConfigXml.CreateNode(System.Xml.XmlNodeType.Element, "FilePath", null);
                        filePathNode.InnerText = filepath;

                        mainConfigXml.SelectSingleNode("Main/PlayList").AppendChild(filePathNode);
                        mainConfigXml.Save(mainConfigXmlFileName);

                        //Select the playlist if it's the only entry on the list
                        if (playlistListBox.Items.Count == 1)
                        {
                            playlistListBox.SelectedIndex = 0;
                        }

                        //Rearrange by alphabetical order                        
                        playlistListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("filepath", System.ComponentModel.ListSortDirection.Ascending));
                    }
                }
            }
        }
/************************************************************************************************/
        private void playlistListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playlistListBox.SelectedIndex == -1)
            {
                return;
            }

            //Load the new songs into the song listbox
            tempListOfFiles.Clear();
            songlistListBox.Items.Clear();
            filterTB.Text = "";

            GetAllFiles(new DirectoryInfo((playlistListBox.SelectedItem as PlayListClass).filepath));
            List<FileInfo> sortedFiles = tempListOfFiles.OrderBy(f => f.Name).ToList();

            if (sortedFiles.Count == 0)
            {
                return;
            }

            //Put the files into the listbox. Check if it's the same playlist, if it is then update the existing list back
            if (nowPlayingList.Count == 0 || sortedFiles[0].DirectoryName != System.IO.Path.GetDirectoryName(nowPlayingList.ElementAt(0).Key))
            {
                //Quite clear, it's a new directory/playlist. Just add normally.
                for (int i = 0; i < sortedFiles.Count(); i++)
                {
                    if (IsAudioFile(sortedFiles[i].Name) == true)
                    {
                        songlistListBox.Items.Add(new SongClass(sortedFiles[i].FullName));
                    }
                }
            }
            else //It is the same playlist. Use back the same objects.
            {
                for (int i = 0; i < sortedFiles.Count(); i++)
                {
                    if (IsAudioFile(sortedFiles[i].Name) == true)
                    {
                        if (nowPlayingList.ContainsKey(sortedFiles[i].FullName) == true)
                        {
                            songlistListBox.Items.Add(nowPlayingList[sortedFiles[i].FullName]);
                        }
                        else
                        {
                            songlistListBox.Items.Add(new SongClass(sortedFiles[i].FullName));
                        }
                    }
                }

                nowPlayingList.Clear();
                foreach (SongClass songObj in songlistListBox.Items)
                {
                    nowPlayingList.Add(songObj.filepath, songObj);
                }
            }

            Console.WriteLine("selection change ended");
        }

        private void refreshPlayListListBox()
        {
            mainConfigXml.Load(mainConfigXmlFileName);

            System.Xml.XmlNodeList playlist = mainConfigXml.SelectNodes("Main/PlayList/FilePath");

            for (int i = 0; i < playlist.Count; i++)
            {
                if (Directory.Exists(playlist[i].InnerText) == true)
                {
                    //Add to the playlist listbox
                    playlistListBox.Items.Add(new PlayListClass(playlist[i].InnerText));
                }
            }

            //Rearrange by alphabetical order                        
            playlistListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("filepath", System.ComponentModel.ListSortDirection.Ascending));

            //Select the playlist if it's the only entry on the list
            if (playlistListBox.Items.Count > 0)
            {
                playlistListBox.SelectedIndex = 0;
            }
        }

        private void GetAllFiles(DirectoryInfo dir)
        {
            foreach (FileInfo fi in dir.GetFiles())
                tempListOfFiles.Add(fi);

            foreach (DirectoryInfo di in dir.GetDirectories())
                GetAllFiles(di);
        }

        private bool IsAudioFile(string path)
        {
            return -1 != Array.IndexOf(audioExtensions, System.IO.Path.GetExtension(path).ToUpperInvariant());
        }
/************************************************************************************************/
        private void playlistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GlobalVariables.NowPlayingSingle = "";

            //Change song list to green
            foreach (SongClass songObj in songlistListBox.Items)
            {
                songObj.isPlaying = false;
            }

            //Change to green color
            foreach (PlayListClass playListObj in playlistListBox.Items)
            {
                playListObj.isPlaying = false;
            }
            (playlistListBox.SelectedItem as PlayListClass).isPlaying = true;

            //Play a song
            if (songlistListBox.Items.Count > 0)
            {
                if (shuffleState == STATE.ON)
                {
                    //If shuffle, random a song
                    songlistListBox.SelectedIndex = new Random().Next(0, songlistListBox.Items.Count - 1);
                }
                else
                {
                    //If no shuffle, play the first song on the list
                    songlistListBox.SelectedIndex = 0;
                }

                playSong(songlistListBox.Items[songlistListBox.SelectedIndex] as SongClass);

                //Scroll into view on the listbox
                new System.Threading.Thread(() =>
                {
                    System.Threading.Thread.Sleep(200);
                    Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                    {
                        scrollNowPlayingSongIntoView();
                    }));
                }).Start();
            }

            //Load the existing list into a backend database
            nowPlayingList.Clear();
            foreach (SongClass songObj in songlistListBox.Items)
            {
                nowPlayingList.Add(songObj.filepath, songObj);
            }
        }

        private void songlistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GlobalVariables.NowPlayingSingle = "";

            //Assign the playlist isPlaying to true
            foreach (PlayListClass playListObj in playlistListBox.Items)
            {
                playListObj.isPlaying = false;
            }
            (playlistListBox.SelectedItem as PlayListClass).isPlaying = true;

            //Create the backend database from zero (because the database might be from a different playlist)
            nowPlayingList.Clear();

            foreach (SongClass songObj in songlistListBox.Items)
            {
                songObj.isPlaying = false;
                nowPlayingList.Add(songObj.filepath, songObj);
            }

            //Play the song 
            playSong(songlistListBox.SelectedItem as SongClass);
        }
/************************************************************************************************/
    }
}
