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
        //Formats the WPF MediaElement (Windows Media Foundation) can decode out of the box.
        //Keep this in step with $Extensions in Scripts\Register-FileAssociations.ps1 - a type
        //registered there but missing here launches the app on a file it will never list.
        private static readonly string[] audioExtensions = { ".MP3", ".WMA", ".M4A", ".AAC", ".WAV", ".FLAC" };

        //Guards against directory junction loops and pathological nesting during a folder scan.
        private const int maxFolderScanDepth = 32;
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
                    if ((playlistListBox.Items[i] as Playlist).FilePath == filepath)
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
                        playlistListBox.Items.Add(new Playlist(filepath));

                        //Add the folder path to the xml database
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
                        playlistListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("FilePath", System.ComponentModel.ListSortDirection.Ascending));
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

            GetAllFiles(new DirectoryInfo((playlistListBox.SelectedItem as Playlist).FilePath));
            List<FileInfo> sortedFiles = tempListOfFiles.OrderBy(f => f.Name).ToList();

            if (sortedFiles.Count == 0)
            {
                return;
            }

            //Put the files into the listbox. Check if it's the same playlist, if it is then update the existing list back
            if (NowPlayingList.Count == 0 || sortedFiles[0].DirectoryName != System.IO.Path.GetDirectoryName(NowPlayingList.ElementAt(0).Key))
            {
                //Quite clear, it's a new directory/playlist. Just add normally.
                for (int i = 0; i < sortedFiles.Count(); i++)
                {
                    if (IsAudioFile(sortedFiles[i].Name) == true)
                    {
                        songlistListBox.Items.Add(new Song(sortedFiles[i].FullName));
                    }
                }
            }
            else //It is the same playlist. Use back the same objects.
            {
                for (int i = 0; i < sortedFiles.Count(); i++)
                {
                    if (IsAudioFile(sortedFiles[i].Name) == true)
                    {
                        if (NowPlayingList.ContainsKey(sortedFiles[i].FullName) == true)
                        {
                            songlistListBox.Items.Add(NowPlayingList[sortedFiles[i].FullName]);
                        }
                        else
                        {
                            songlistListBox.Items.Add(new Song(sortedFiles[i].FullName));
                        }
                    }
                }

                NowPlayingList.Clear();
                foreach (Song songObj in songlistListBox.Items)
                {
                    NowPlayingList.Add(songObj.FilePath, songObj);
                }
            }
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
                    playlistListBox.Items.Add(new Playlist(playlist[i].InnerText));
                }
            }

            //Rearrange by alphabetical order                        
            playlistListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("FilePath", System.ComponentModel.ListSortDirection.Ascending));

            //Select the playlist if it's the only entry on the list
            if (playlistListBox.Items.Count > 0)
            {
                playlistListBox.SelectedIndex = 0;
            }
        }

        //Walks dir and every subfolder, collecting files into tempListOfFiles.
        //Runs synchronously on the UI thread, so it must never throw: a single unreadable
        //subfolder, a dead junction or an over-long path would otherwise take the app down.
        private void GetAllFiles(DirectoryInfo dir, int depth = 0)
        {
            if (depth > maxFolderScanDepth)
            {
                return;
            }

            FileInfo[] files;
            DirectoryInfo[] subDirectories;

            try
            {
                files = dir.GetFiles();
                subDirectories = dir.GetDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                //Permission denied on this folder - skip it and carry on with the rest.
                return;
            }
            catch (IOException)
            {
                //Covers DirectoryNotFoundException and PathTooLongException among others.
                return;
            }

            foreach (FileInfo fi in files)
                tempListOfFiles.Add(fi);

            foreach (DirectoryInfo di in subDirectories)
            {
                //A directory symlink or junction can point back up its own tree; following one
                //would recurse until the stack runs out.
                if ((di.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    continue;
                }

                GetAllFiles(di, depth + 1);
            }
        }

        private bool IsAudioFile(string path)
        {
            return -1 != Array.IndexOf(audioExtensions, System.IO.Path.GetExtension(path).ToUpperInvariant());
        }
/************************************************************************************************/
        private void playlistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Called with null arguments from nextGrid_Click and playGrid_Click, where there is
            //no guarantee a playlist is selected - after deleting the last one, for instance.
            Playlist selectedPlayListObj = playlistListBox.SelectedItem as Playlist;

            if (selectedPlayListObj == null)
            {
                return;
            }

            GlobalVariables.NowPlayingSingle = "";

            //Change song list to green
            foreach (Song songObj in songlistListBox.Items)
            {
                songObj.IsPlaying = false;
            }

            //Change to green color
            foreach (Playlist playListObj in playlistListBox.Items)
            {
                playListObj.IsPlaying = false;
            }
            selectedPlayListObj.IsPlaying = true;

            //Play a song
            if (songlistListBox.Items.Count > 0)
            {
                if (ShuffleState == ShuffleState.On)
                {
                    //If shuffle, random a song. Next(count) is exclusive on the upper bound.
                    songlistListBox.SelectedIndex = shuffleRandom.Next(songlistListBox.Items.Count);
                }
                else
                {
                    //If no shuffle, play the first song on the list
                    songlistListBox.SelectedIndex = 0;
                }

                PlaySong(songlistListBox.Items[songlistListBox.SelectedIndex] as Song);

                //Scroll into view on the listbox, once the list has had a chance to lay out
                //the containers for the items that were just added.
                invokeAfter(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, scrollNowPlayingSongIntoView);
            }

            //Load the existing list into a backend database
            NowPlayingList.Clear();
            foreach (Song songObj in songlistListBox.Items)
            {
                NowPlayingList.Add(songObj.FilePath, songObj);
            }
        }

        private void songlistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Called with null arguments from PlaySong(string), so neither selection is certain.
            Song selectedSongObj = songlistListBox.SelectedItem as Song;

            if (selectedSongObj == null)
            {
                return;
            }

            GlobalVariables.NowPlayingSingle = "";

            //Assign the playlist IsPlaying to true
            foreach (Playlist playListObj in playlistListBox.Items)
            {
                playListObj.IsPlaying = false;
            }

            Playlist selectedPlayListObj = playlistListBox.SelectedItem as Playlist;

            if (selectedPlayListObj != null)
            {
                selectedPlayListObj.IsPlaying = true;
            }

            //Create the backend database from zero (because the database might be from a different playlist)
            NowPlayingList.Clear();

            foreach (Song songObj in songlistListBox.Items)
            {
                songObj.IsPlaying = false;
                NowPlayingList.Add(songObj.FilePath, songObj);
            }

            //Play the song
            PlaySong(selectedSongObj);
        }
/************************************************************************************************/
    }
}
