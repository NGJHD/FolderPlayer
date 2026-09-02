using System;
using System.Windows;
using System.Windows.Threading;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
/************************************************************************************************/
        private DispatcherTimer seekBarTimer = new DispatcherTimer(DispatcherPriority.Render);
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
                currentTimeTextBlock.Text = ts.ToString(@"mm\:ss");
            }
            else
            {
                currentTimeTextBlock.Text = ts.ToString(@"hh\:mm\:ss");
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
                if (RepeatMode == RepeatMode.Single || RepeatMode == RepeatMode.Playlist)
                {
                    PlaySong(new Song(GlobalVariables.NowPlayingSingle));
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
                //Let the MediaElement settle after the Stop() above before loading the next
                //source, otherwise the new track can be cut off as it starts.
                invokeAfter(TimeSpan.FromMilliseconds(250), DispatcherPriority.Render, () => nextGrid_Click(null, null));
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

            if (ts.Hours != 0)
            {
                totalTimeTextBlock.Text = ts.ToString(@"hh\:mm\:ss");
                hasHour = true;
            }
            else
            {
                totalTimeTextBlock.Text = ts.ToString(@"mm\:ss");
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
