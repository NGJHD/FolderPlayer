using System.ComponentModel;

namespace AudioPlayer
{
    public class Song : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _filePath;
        public string FilePath
        {
            get
            {
                return _filePath;
            }

            set
            {
                _filePath = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("FilePath"));
                }
            }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }

            set
            {
                _isPlaying = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsPlaying"));
                }
            }
        }

        public Song()
        {
        }

        public Song(string filePath)
        {
            FilePath = filePath;
            IsPlaying = false;
        }
    }
}
