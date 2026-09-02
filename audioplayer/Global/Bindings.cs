using System.Windows;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
        public ShuffleState ShuffleState
        {
            get { return (ShuffleState)GetValue(ShuffleStateProperty); }
            set { SetValue(ShuffleStateProperty, value); }
        }
        public static readonly DependencyProperty ShuffleStateProperty = DependencyProperty.Register("ShuffleState", typeof(ShuffleState), typeof(Window), new PropertyMetadata(ShuffleState.Off));

        public RepeatMode RepeatMode
        {
            get { return (RepeatMode)GetValue(RepeatModeProperty); }
            set { SetValue(RepeatModeProperty, value); }
        }
        public static readonly DependencyProperty RepeatModeProperty = DependencyProperty.Register("RepeatMode", typeof(RepeatMode), typeof(Window), new PropertyMetadata(RepeatMode.Playlist));
    }
}
