using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfImageViewer
{
    public enum VisualStates
    {
        ShowImage,
        ShowMedia
    }

    public class MediaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string filenameImage;
        private string filenameMedia;
        private VisualStates currentVisualState;

        public string FilenameImage
        {
            get => filenameImage;
            set
            {
                filenameImage = value;
                OnPropertyChanged();
            }
        }

        public string FilenameMedia
        {
            get => filenameMedia;
            set
            {
                filenameMedia = value;
                OnPropertyChanged();
            }
        }

        public VisualStates CurrentVisualState
        {
            get => currentVisualState;
            set
            {
                currentVisualState = value;
                OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
