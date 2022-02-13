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
        protected int imageWidth;
        protected int imageHeight;
        protected ImageSource image;

        public int ImageWidth { get { return this.imageWidth; } set { this.imageWidth = value; OnPropertyChanged("ImageWidth"); } }
        public int ImageHeight { get { return this.imageHeight; } set { this.imageHeight = value; OnPropertyChanged("ImageHeight"); } }
        public ImageSource Image { get { return this.image; } set { this.image = value; OnPropertyChanged("Image"); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public abstract void OnImageNewSizeRequest(int width, int height);
    }
}
