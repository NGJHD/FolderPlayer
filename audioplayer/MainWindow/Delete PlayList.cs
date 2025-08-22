using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AudioPlayer
{
    public partial class MainWindow : Window
    {
/************************************************************************************************/
        private bool mouseDown = false;
        private Point originalPt = default(Point);
        private bool deleteIsVisible = false;
/************************************************************************************************/
        private void deleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //Remove from XML
            PlayListClass playListObj = (PlayListClass)playlistListBox.SelectedItem;
            System.Xml.XmlNodeList playlist = mainConfigXml.SelectNodes("Main/PlayList/FilePath");

            for (int i = 0; i < playlist.Count; i++)
            {
                if (playListObj.filepath == playlist[i].InnerText)
                {
                    mainConfigXml.SelectSingleNode("Main/PlayList").RemoveChild(playlist[i]);
                    mainConfigXml.Save(mainConfigXmlFileName);

                    break;
                }
            }

            //Remove from ListBox
            int deleteIdx = playlistListBox.SelectedIndex;

            if (playlistListBox.Items.Count == 1)
            {
                playlistListBox.SelectedIndex = -1;
                songlistListBox.Items.Clear();
            }
            else
            {
                if (playlistListBox.SelectedIndex == playlistListBox.Items.Count - 1)
                {
                    playlistListBox.SelectedIndex = 0;
                }
                else
                {
                    playlistListBox.SelectedIndex += 1;
                }
            }

            playlistListBox.Items.RemoveAt(deleteIdx);
        }
/************************************************************************************************/
        private void mainGrid_PreviewInputDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            originalPt = e.GetPosition(this);
            ClickGrid deleteButton = (ClickGrid)((Grid)sender).FindName("deleteButton");

            if (deleteButton == null)
            {
                return;
            }

            if (deleteButton.Width != 0)
            {
                deleteIsVisible = true;
            }
            else
            {
                deleteIsVisible = false;
            }
        }

        private void mainGrid_PreviewInputMove(object sender, MouseEventArgs e)
        {
            if (mouseDown == true)
            {
                Point currPt = e.GetPosition(this);
                ClickGrid deleteButton = (ClickGrid)((Grid)sender).FindName("deleteButton");

                if (deleteButton == null)
                {
                    return;
                }

                double diffX = currPt.X - originalPt.X;

                if (deleteIsVisible == true)
                {
                    if (diffX == 0)
                    {
                        return;
                    }
                    else if (diffX < -60)
                    {
                        deleteButton.Width = 0;
                    }
                    else if (diffX > 0)
                    {
                        deleteButton.Width = 60;
                    }
                    else
                    {
                        deleteButton.Width = 60 + diffX;
                    }
                }
                else
                {
                    if (diffX == 0)
                    {
                        return;
                    }
                    else if (diffX > 60)
                    {
                        diffX = 60;
                    }
                    else if (diffX < 0)
                    {
                        diffX = 0;
                    }

                    deleteButton.Width = diffX;
                }
            }
        }

        private void mainGrid_PreviewTouchUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
            ClickGrid deleteButton = (ClickGrid)((Grid)sender).FindName("deleteButton");

            if (deleteButton == null)
            {
                return;
            }

            if (deleteButton.Width != 60)
            {
                System.Windows.Media.Animation.DoubleAnimation hideDeleteButtonAnimation = new System.Windows.Media.Animation.DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(250)));// AnimationClass.Duration_250ms);
                hideDeleteButtonAnimation.Completed += (o2, e2) =>
                {
                    deleteButton.BeginAnimation(FrameworkElement.WidthProperty, null);
                    deleteButton.Width = 0;
                };

                deleteButton.BeginAnimation(FrameworkElement.WidthProperty, hideDeleteButtonAnimation);
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            deleteMenuItem_Click(null, null);
        }
/************************************************************************************************/
    }
}
