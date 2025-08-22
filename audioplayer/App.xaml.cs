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
            //MainWindow window = new MainWindow();
            GlobalVariables.mainWindow = new MainWindow();
            GlobalVariables.mainWindow.Show();

            if (e.Args.Length > 0)
            {
                GlobalVariables.NowPlayingSingle = e.Args[0];
                GlobalVariables.mainWindow.playSong(new SongClass(e.Args[0]));
            }
        }        

        bool ISingleInstanceApp.SignalExternalCommandLineArgs(IList<string> args)
        {
            if (args.Count == 1)
            {
                GlobalVariables.mainWindow.ShowInTaskbar = true;
                GlobalVariables.mainWindow.WindowState = WindowState.Normal;
                GlobalVariables.mainWindow.Activate();
            }

            if (args.Count == 2)
            {
                foreach (PlayListClass playListObj in GlobalVariables.mainWindow.playlistListBox.Items)
                {
                    playListObj.isPlaying = false;
                }

                for (int i = 0; i < GlobalVariables.mainWindow.nowPlayingList.Count; i++)
                {
                    SongClass songObj = GlobalVariables.mainWindow.nowPlayingList.ElementAt(i).Value as SongClass;

                    if (songObj.isPlaying == true)
                    {
                        songObj.isPlaying = false;
                    }
                }

                GlobalVariables.NowPlayingSingle = args[1];
                GlobalVariables.mainWindow.playSong(new SongClass(args[1]));
            }

            return true;
        }
/************************************************************************************************/
    }
}
