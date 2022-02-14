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
    public abstract class ImageSourceWrapper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSource Source { get { return this._source; } protected set { this._source = value; OnPropertyChanged("Source"); } }

        #pragma warning disable CS8618
        private ImageSource _source;

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}