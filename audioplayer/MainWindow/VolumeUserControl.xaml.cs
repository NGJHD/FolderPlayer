using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AudioPlayer
{
    public partial class VolumeUserControl : UserControl
    {
        SolidColorBrush activeBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#00FFFF");
        SolidColorBrush inactiveBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#294c67");

        public VolumeUserControl()
        {
            InitializeComponent();
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                int level = Convert.ToInt32((sender as Border).Name.Replace("level", ""));
                SetLevel(level);
            }
        }

        /// <summary>
        /// Audio levels are 0 (mute) to 10 (max)
        /// </summary>
        /// <param name="level"></param>
        public void SetLevel(int level)
        {
            for (int i = mainGrid.Children.Count - 1; i >= 0; i--)
            {
                Border myBorder = mainGrid.Children[i] as Border;
                int borderLevel = Convert.ToInt32(myBorder.Name.Replace("level", ""));
                if (borderLevel != 0)
                {
                    myBorder.Background = (borderLevel <= level ? activeBrush : inactiveBrush);
                }
            }

            GlobalVariables.MainWindow.mediaPlayer.Volume = level / 10.0;
        }

        public int GetLevel()
        {
            for (int i = 0; i < mainGrid.Children.Count - 1; i++)
            {
                Border myBorder = mainGrid.Children[i] as Border;
                if (myBorder.Background == activeBrush)
                {
                    return Convert.ToInt32(myBorder.Name.Replace("level", ""));
                }
            }

            return 0;
        }
    }
}
