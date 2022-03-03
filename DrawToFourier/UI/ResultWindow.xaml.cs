using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DrawToFourier.UI
{
    public partial class ResultWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSourceWrapper ImageSourceWrapper
        {
            get { return this._imageSourceWrapper; }
            set { this._imageSourceWrapper = value; OnPropertyChanged("ImageSourceWrapper"); }
        }

        public DrawAreaSize DrawAreaSize
        {
            get { return this._drawAreaSize; }
            set { this._drawAreaSize = value; OnPropertyChanged("DrawAreaSize"); }
        }

        private ImageSourceWrapper _imageSourceWrapper;
        private DrawAreaSize _drawAreaSize;

        public ResultWindow(ImageSourceWrapper imageSourceWrapper, int desiredDrawAreaWidth, int desiredDrawAreaHeight)
        {
            this.ImageSourceWrapper = imageSourceWrapper;
            this.DrawAreaSize = new DrawAreaSize(desiredDrawAreaWidth, desiredDrawAreaHeight);
            InitializeComponent();
        }

        private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DrawAreaSize.NewSize((int)e.NewSize.Width, (int)(e.NewSize.Height));
            OnPropertyChanged("DrawAreaSize");
        }

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
