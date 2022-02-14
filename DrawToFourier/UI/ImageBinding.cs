using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawToFourier.UI
{
    public abstract class ImageBinding : INotifyPropertyChanged
    {
        #pragma warning disable CS8618
        private int _desiredWidth;
        private int _desiredHeight;
        private ImageSource _imageSource;

        public int DesiredWidth { get { return this._desiredWidth; } set { this._desiredWidth = value; OnPropertyChanged("DesiredWidth"); } }
        public int DesiredHeight { get { return this._desiredHeight; } set { this._desiredHeight = value; OnPropertyChanged("DesiredHeight"); } }
        public ImageSource ImageSource { get { return this._imageSource; } set { this._imageSource = value; OnPropertyChanged("ImageSource"); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public void NewSizeRequest(int width, int height)
        {
            this.DesiredWidth = width;
            this.DesiredHeight = height;
        }
    }
}