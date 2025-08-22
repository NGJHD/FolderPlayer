using System.ComponentModel;

namespace AudioPlayer
{
    class PlayListClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _filepath;
        public string filepath
        {
            get
            {
                return _filepath;
            }

            set
            {
                _filepath = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("filepath"));
                }
            }
        }

        private bool _isPlaying;
        public bool isPlaying
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
                    PropertyChanged(this, new PropertyChangedEventArgs("isPlaying"));
                }
            }
        }

        public PlayListClass()
        {
        }

        public PlayListClass(string filepath)
        {
            this.filepath = filepath;
            isPlaying = false;
        }
    }
}
