using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Shell;

namespace AudioPlayer
{
    public partial class App : Application, ISingleInstanceApp
    {
/************************************************************************************************/
        private const string Unique = "FOLDER_PLAYER_UNIQUE_STRING";
/************************************************************************************************/
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique)) //first instance
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();
                SingleInstance<App>.Cleanup();
            }
        }

        [STAThread]
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            GlobalVariables.MainWindow = new MainWindow();
            GlobalVariables.MainWindow.Show();

            if (e.Args.Length > 0)
            {
                GlobalVariables.NowPlayingSingle = e.Args[0];
                GlobalVariables.MainWindow.PlaySong(new Song(e.Args[0]));
            }
        }        

        bool ISingleInstanceApp.SignalExternalCommandLineArgs(IList<string> args)
        {
            if (args.Count == 1)
            {
                GlobalVariables.MainWindow.ShowInTaskbar = true;
                GlobalVariables.MainWindow.WindowState = WindowState.Normal;
                GlobalVariables.MainWindow.Activate();
            }

            if (args.Count == 2)
            {
                foreach (Playlist playListObj in GlobalVariables.MainWindow.playlistListBox.Items)
                {
                    playListObj.IsPlaying = false;
                }

                for (int i = 0; i < GlobalVariables.MainWindow.NowPlayingList.Count; i++)
                {
                    Song songObj = GlobalVariables.MainWindow.NowPlayingList.ElementAt(i).Value as Song;

                    if (songObj.IsPlaying == true)
                    {
                        songObj.IsPlaying = false;
                    }
                }

                GlobalVariables.NowPlayingSingle = args[1];
                GlobalVariables.MainWindow.PlaySong(new Song(args[1]));
            }

            return true;
        }
/************************************************************************************************/
    }
}
