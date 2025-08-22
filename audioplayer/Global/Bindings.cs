using System.Windows;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
        public STATE shuffleState
        {
            get { return (STATE)GetValue(shuffleStateProperty); }
            set { SetValue(shuffleStateProperty, value); }
        }
        public static readonly DependencyProperty shuffleStateProperty = DependencyProperty.Register("shuffleState", typeof(STATE), typeof(Window), new PropertyMetadata(STATE.OFF));

        public REPEAT_MODE repeatMode
        {
            get { return (REPEAT_MODE)GetValue(repeatModeProperty); }
            set { SetValue(repeatModeProperty, value); }
        }
        public static readonly DependencyProperty repeatModeProperty = DependencyProperty.Register("repeatMode", typeof(REPEAT_MODE), typeof(Window), new PropertyMetadata(REPEAT_MODE.PLAYLIST));
    }
}
