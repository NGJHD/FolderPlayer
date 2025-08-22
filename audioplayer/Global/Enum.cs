using System.Windows;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
        public enum STATE
        {
            ON,
            OFF
        }

        public enum REPEAT_MODE
        {
            OFF,
            PLAYLIST,
            SINGLE
        }
    }
}
