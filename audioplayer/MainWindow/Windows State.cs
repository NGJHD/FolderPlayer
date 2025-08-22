using System;
using System.Windows;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
/************************************************************************************************/
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenu trayMenu;
/************************************************************************************************/
        private void initTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Text = "Folder Player";
            trayIcon.Icon = new System.Drawing.Icon(AudioPlayer.Properties.Resources.Player, 40, 40);
            trayIcon.Visible = true;

            trayMenu = new System.Windows.Forms.ContextMenu();
            trayIcon.ContextMenu = trayMenu;

            trayIcon.DoubleClick += trayIcon_DoubleClick;
            trayIcon.ContextMenu.Popup += PopulateContextMenu;
        }

        private void PopulateContextMenu(object sender, EventArgs e)
        {
            // Empty menu to prevent stuff to pile up
            trayMenu.MenuItems.Clear();

            if (String.IsNullOrWhiteSpace(nowPlayingSong) == false)
            {
                trayMenu.MenuItems.Add("Now Playing:");
                trayMenu.MenuItems.Add("        " + System.IO.Path.GetFileName(nowPlayingSong));
                trayMenu.MenuItems[0].Enabled = false;
                trayMenu.MenuItems[1].Enabled = false;
            }

            var closeItem = new System.Windows.Forms.MenuItem { Text = "Close" };
            closeItem.Click += OnClose;
            trayMenu.MenuItems.Add(closeItem);
        }
/************************************************************************************************/
        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = System.Windows.WindowState.Normal;
            this.Activate();
            //trayIcon.Visible = false;
        }

        private void mainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                //trayIcon.Visible = true;
            }
        }
/************************************************************************************************/
        private void OnClose(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
/************************************************************************************************/
    }
}
