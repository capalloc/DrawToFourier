using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawToFourier.UI
{
    // Provides an abstraction layer for providing means of transmitting mouse actions to custom implementations of image/canvas handling and using custom image sources.
    public abstract class ImageSourceWrapper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSource Source { get { return this._source; } protected set { this._source = value; OnPropertyChanged("Source"); } }

        #pragma warning disable CS8618
        private ImageSource _source;

        public abstract void OnMouseDown(double X, double Y, MouseButton clicked);
        public abstract void OnMouseLeave(double X, double Y);
        public abstract void OnMouseEnter(double X, double Y);
        public abstract void OnMouseMove(double X, double Y);

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}