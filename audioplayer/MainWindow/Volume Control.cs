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
        private void volumeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = volumeBar.Value / 100;
        }

        private void VolumeMin_MouseDown(object sender, MouseButtonEventArgs e)
        {
            volumeBar.Value -= 10;
        }

        private void VolumeMax_MouseDown(object sender, MouseButtonEventArgs e)
        {
            volumeBar.Value += 10;
        }
/************************************************************************************************/
    }
}
