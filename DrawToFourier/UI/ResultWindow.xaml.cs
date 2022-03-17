using CustomDrawingWithWPF;
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

        public ImageSourceWrapper ImageSourceWrapper { get { return this._imageSourceWrapper; } set { this._imageSourceWrapper = value; OnPropertyChanged("ImageSourceWrapper"); } }
        public DrawAreaSize DrawAreaSize { get { return this._drawAreaSize; } set { this._drawAreaSize = value; OnPropertyChanged("DrawAreaSize"); } }

        public double ZoomScale { get { return this._zoomScale; } set { this._zoomScale = value; OnPropertyChanged("ZoomScale"); } }
        public double ZoomCenterX { get { return this._zoomCenterX; } set { this._zoomCenterX = value; OnPropertyChanged("ZoomCenterX"); } }
        public double ZoomCenterY { get { return this._zoomCenterY; } set { this._zoomCenterY = value; OnPropertyChanged("ZoomCenterY"); } }
        public double ZoomTranslateX { get { return this._zoomTranslateX; } set { this._zoomTranslateX = value; OnPropertyChanged("ZoomTranslateX"); } }
        public double ZoomTranslateY { get { return this._zoomTranslateY; } set { this._zoomTranslateY = value; OnPropertyChanged("ZoomTranslateY"); } }

        private ImageSourceWrapper _imageSourceWrapper;
        private DrawAreaSize _drawAreaSize;

        private double _zoomScale;
        private double _zoomMultiplier;
        private double _zoomCenterX;
        private double _zoomCenterY;
        private double _zoomTranslateX;
        private double _zoomTranslateY;

        private double _screenOriginX;
        private double _screenOriginY;
        private double _screenEndX;
        private double _screenEndY;

        public ResultWindow(ImageSourceWrapper imageSourceWrapper, int desiredDrawAreaWidth, int desiredDrawAreaHeight)
        {
            this.ImageSourceWrapper = imageSourceWrapper;
            this.DrawAreaSize = new DrawAreaSize(desiredDrawAreaWidth, desiredDrawAreaHeight);
            this.ZoomScale = 1;
            this._zoomMultiplier = 1.1;
            this.ZoomCenterX = 0.5;
            this.ZoomCenterY = 0.5;
            this.ZoomTranslateX = 0;
            this.ZoomTranslateY = 0;

            this._screenOriginX = 0;
            this._screenOriginY = 0;
            this._screenEndX = 1;
            this._screenEndY = 1;

            InitializeComponent();
            this.ImageSourceWrapper.Source.Changed += (sender, e) => { this.ResultImageContainer.InvalidateVisual(); };
        }

        private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DrawAreaSize.NewSize((int)e.NewSize.Width, (int)(e.NewSize.Height));
            OnPropertyChanged("DrawAreaSize");
        }

        private void ResultImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double prevScale = this.ZoomScale;

            if (e.Delta < 0 && this.ZoomScale / this._zoomMultiplier < 1 || e.Delta == 0)
            {
                this.ZoomScale = 1;
                this.ZoomCenterX = 0.5;
                this.ZoomCenterY = 0.5;
                this.ZoomTranslateX = 0;
                this.ZoomTranslateY = 0;
                this._screenOriginX = 0;
                this._screenOriginY = 0;
                this._screenEndX = 1;
                this._screenEndY = 1;
                return;
            }
            else if (e.Delta < 0)
            {
                this.ZoomScale /= this._zoomMultiplier;
            }
            else
            {
                this.ZoomScale *= this._zoomMultiplier;
            }

            double centerLocalX = e.GetPosition(this.ResultImageContainer).X / this.DrawAreaSize.Width;
            double centerLocalY = e.GetPosition(this.ResultImageContainer).Y / this.DrawAreaSize.Height;

            this.ZoomTranslateX = centerLocalX - this.ZoomCenterX - (centerLocalX - this.ZoomCenterX - this.ZoomTranslateX) / prevScale;
            this.ZoomTranslateY = centerLocalY - this.ZoomCenterY - (centerLocalY - this.ZoomCenterY - this.ZoomTranslateY) / prevScale;

            this.ZoomCenterX = centerLocalX - this.ZoomTranslateX;
            this.ZoomCenterY = centerLocalY - this.ZoomTranslateY;

            this._screenOriginX = this.ZoomCenterX - centerLocalX / this.ZoomScale;
            this._screenOriginY = this.ZoomCenterY - centerLocalY / this.ZoomScale;
            this._screenEndX = this._screenOriginX + 1 / this.ZoomScale;
            this._screenEndY = this._screenOriginY + 1 / this.ZoomScale;

            double totalChangeToScreenX = 0;
            double totalChangeToScreenY = 0;

            if (this._screenOriginX < 0)
            {
                totalChangeToScreenX -= this._screenOriginX;
            }
            if (this._screenOriginY < 0)
            {
                totalChangeToScreenY -= this._screenOriginY;
            }
            if (this._screenEndX + totalChangeToScreenX > 1)
            {
                totalChangeToScreenX -= this._screenEndX + totalChangeToScreenX - 1;
            }
            if (this._screenEndY + totalChangeToScreenY > 1)
            {
                totalChangeToScreenY -= this._screenEndY + totalChangeToScreenY - 1;
            }

            ZoomTranslateX -= totalChangeToScreenX * this.ZoomScale;
            ZoomTranslateY -= totalChangeToScreenY * this.ZoomScale;
            this._screenOriginX += totalChangeToScreenX;
            this._screenOriginY += totalChangeToScreenY;
            this._screenEndX += totalChangeToScreenX;
            this._screenEndY += totalChangeToScreenY;
        }

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
