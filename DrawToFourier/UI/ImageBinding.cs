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
        private int _imageWidth;
        private int _imageHeight;
        private ImageSource _image;

        public int ImageWidth { get { return this._imageWidth; } set { this._imageWidth = value; this.OnPropertyChanged("ImageWidth"); } }
        public int ImageHeight { get { return this._imageHeight; } set { this._imageHeight = value; this.OnPropertyChanged("ImageHeight"); } }
        public ImageSource Image { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            System.Diagnostics.Debug.WriteLine($"{ImageWidth} {ImageHeight}");
            if (PropertyChanged != null)
            {
                System.Diagnostics.Debug.WriteLine($"{ImageWidth} {ImageHeight}");
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
