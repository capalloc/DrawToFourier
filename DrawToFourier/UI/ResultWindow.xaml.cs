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

namespace DrawToFourier.UI
{
    public partial class ResultWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSourceWrapper ImageWrapper
        {
            get { return this._imageWrapper; }
        }

        // Desired size properties represent the target draw area size based on current window size
        public int DesiredDrawAreaWidth
        {
            get { return this._desiredDrawAreaWidth; }
            set { this._desiredDrawAreaWidth = value; OnPropertyChanged("DesiredDrawAreaWidth"); }
        }
        public int DesiredDrawAreaHeight
        {
            get { return this._desiredDrawAreaHeight; }
            set { this._desiredDrawAreaHeight = value; OnPropertyChanged("DesiredDrawAreaHeight"); }
        }

        // Normal size properties represent the actual target draw area size based on current window size
        public int DrawAreaWidth
        {
            get { return this._drawAreaWidth; }
            set { this._drawAreaWidth = value; OnPropertyChanged("DrawAreaWidth"); }
        }
        public int DrawAreaHeight
        {
            get { return this._drawAreaHeight; }
            set { this._drawAreaHeight = value; OnPropertyChanged("DrawAreaHeight"); }
        }

        private ImageSourceWrapper _imageWrapper;
        private int _desiredDrawAreaWidth;
        private int _desiredDrawAreaHeight;
        private int _drawAreaWidth;
        private int _drawAreaHeight;

        public ResultWindow(ImageSourceWrapper imageWrapper, int desiredDrawAreaWidth, int desiredDrawAreaHeight)
        {
            this._imageWrapper = imageWrapper;
            this.DesiredDrawAreaWidth = desiredDrawAreaWidth;
            this.DesiredDrawAreaHeight = desiredDrawAreaHeight;
            this.DrawAreaWidth = this.DrawAreaHeight = Math.Min(desiredDrawAreaHeight, desiredDrawAreaWidth);
            InitializeComponent();
        }

        private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DesiredDrawAreaWidth = (int)e.NewSize.Width;
            this.DesiredDrawAreaHeight = (int)(e.NewSize.Height);
            this.DrawAreaWidth = this.DrawAreaHeight = Math.Min(this.DesiredDrawAreaHeight, this.DesiredDrawAreaWidth);
        }

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
