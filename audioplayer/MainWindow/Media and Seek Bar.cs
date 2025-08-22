using System;
using System.Windows;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
/************************************************************************************************/
        private System.Windows.Threading.DispatcherTimer seekBarTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
        private bool hasHour = false;
/************************************************************************************************/
        private void initSeekBar()
        {
            seekBarTimer.Interval = TimeSpan.FromMilliseconds(200);
            seekBarTimer.Tick += new EventHandler(seekBarTimer_Tick);
        }

        private void seekBarTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = mediaPlayer.Position;
            seekBar.Value = ts.TotalSeconds;

            if (hasHour == false)
            {
                currentTimeTextBlock.Text = ts.ToString(@"mm\:ss"); //ts.Minutes.ToString() + ":" + ts.Seconds.ToString().PadLeft(2, '0');
            }
            else
            {
                currentTimeTextBlock.Text = ts.ToString(@"hh\:mm\:ss"); //ts.Hours.ToString().PadLeft(1, '0') + ":" + ts.Minutes.ToString().PadLeft(2, '0') + ":" + ts.Seconds.ToString().PadLeft(2, '0');
            }
        }

        private void seekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (seekBar.Value < mediaPlayer.Position.TotalSeconds - 1 ||
                seekBar.Value > mediaPlayer.Position.TotalSeconds + 1)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(seekBar.Value);
            }
        }
/************************************************************************************************/
        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();

            if (GlobalVariables.NowPlayingSingle != "")
            {
                if (repeatMode == REPEAT_MODE.SINGLE || repeatMode == REPEAT_MODE.PLAYLIST)
                {
                    playSong(new SongClass(GlobalVariables.NowPlayingSingle));
                }
                else
                {
                    seekBar.Value = 0;
                    playImage.Visibility = System.Windows.Visibility.Visible;
                    pauseImage.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                new System.Threading.Thread(() =>
                {
                    System.Threading.Thread.Sleep(250);

                    Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                    {
                        nextGrid_Click(null, null);
                    }));
                }).Start();
            }
        }

        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            bool setElapsedTime = justStarted_SeekBar;

            if (justStarted_SeekBar == false || justStarted_Play == true)
            {
                mediaPlayer.Play();
                justStarted_Play = false;
            }
            else
            {
                mediaPlayer.Pause();
                playImage.Visibility = System.Windows.Visibility.Visible;
                pauseImage.Visibility = System.Windows.Visibility.Collapsed;
            }

            justStarted_SeekBar = false;

            TimeSpan ts = mediaPlayer.NaturalDuration.TimeSpan;
            seekBar.Maximum = ts.TotalSeconds;
            //seekBar.LargeChange = Math.Min(10, ts.Seconds / 10);

            if (ts.Hours != 0)
            {
                totalTimeTextBlock.Text = ts.ToString(@"hh\:mm\:ss"); //ts.Hours + ":" + ts.Minutes.ToString().PadLeft(2, '0') + ":" + ts.Seconds.ToString().PadLeft(2, '0');
                hasHour = true;
            }
            else
            {
                totalTimeTextBlock.Text = ts.ToString(@"mm\:ss"); //ts.Minutes + ":" + ts.Seconds.ToString().PadLeft(2, '0');
                hasHour = false;
            }

            seekBarTimer.Start();

            if (setElapsedTime == true)
            {
                seekBar.Value = Convert.ToDouble(mainConfigXml.SelectSingleNode("Main/LastKnown/ElapsedTime").InnerText);
            }
        }
/************************************************************************************************/
    }
}
