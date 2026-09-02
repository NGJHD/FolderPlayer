using System.ComponentModel;

namespace AudioPlayer
{
    class Playlist : INotifyPropertyChanged
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

        public Playlist()
        {
        }

        public Playlist(string filePath)
        {
            FilePath = filePath;
            IsPlaying = false;
        }
    }
}
