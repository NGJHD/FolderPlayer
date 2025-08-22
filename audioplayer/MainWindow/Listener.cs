using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using TestKeybdHook;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

namespace AudioPlayer
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
/************************************************************************************************/
        private IPEndPoint udpListenIPEndPoint;
        private UdpClient udpListenClient;
        public static int PORT = 54370;
/************************************************************************************************/
        private void udpListener()
        {
            udpListenClient = new UdpClient(PORT);
            udpListenIPEndPoint = new IPEndPoint(IPAddress.Any, PORT);

            while (true)
            {
                try
                {
                    //Receive the data
                    byte[] msg = udpListenClient.Receive(ref udpListenIPEndPoint);
                    int msgID = BitConverter.ToInt32(msg, 0);
                    int msgLength = BitConverter.ToInt32(msg, 4);

                    if (msgID == 1000)
                    {
                        Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                        {
                            foreach (PlayListClass playListObj in playlistListBox.Items)
                            {
                                playListObj.isPlaying = false;
                            }

                            for (int i = 0; i < nowPlayingList.Count; i++)
                            {
                                SongClass songObj = nowPlayingList.ElementAt(i).Value as SongClass;

                                if (songObj.isPlaying == true)
                                {
                                    songObj.isPlaying = false;
                                }
                            }

                            NowPlayingSingle = System.Text.Encoding.Unicode.GetString(msg, 8, 254).Replace("\0", "");
                            playSong(new SongClass(NowPlayingSingle));
                        }));
                    }
                }
                catch (Exception)
                {
                }
            }
        }
/************************************************************************************************/
    }
}
