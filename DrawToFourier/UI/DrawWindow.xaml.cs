using DrawToFourier.Fourier;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static DrawToFourier.Fourier.FourierCore;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DrawToFourier.UI
{
    public partial class DrawWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public ImageSourceWrapper ImageSourceWrapper { get { return this._imageSourceWrapper; } set { this._imageSourceWrapper = value; OnPropertyChanged("ImageSourceWrapper"); } }
        public DrawAreaSize DrawAreaSize { get { return this._drawAreaSize; } set { this._drawAreaSize = value; OnPropertyChanged("DrawAreaSize"); } }

        public double ZoomScale { get { return this._zoomScale; } set { this._zoomScale = value; OnPropertyChanged("ZoomScale"); } }
        public double ZoomCenterX { get { return this._zoomCenterX; } set { this._zoomCenterX = value; OnPropertyChanged("ZoomCenterX"); } }
        public double ZoomCenterY { get { return this._zoomCenterY; } set { this._zoomCenterY = value; OnPropertyChanged("ZoomCenterY"); } }
        public double ZoomTranslateX { get { return this._zoomTranslateX; } set { this._zoomTranslateX = value; OnPropertyChanged("ZoomTranslateX"); } }
        public double ZoomTranslateY { get { return this._zoomTranslateY; } set { this._zoomTranslateY = value; OnPropertyChanged("ZoomTranslateY"); } }

        public bool LoadButtonEnabled { get { return this._loadButtonEnabled; } set { this._loadButtonEnabled = value; OnPropertyChanged("LoadButtonEnabled"); } }
        public bool SaveButtonEnabled { get { return this._saveButtonEnabled; } set { this._saveButtonEnabled = value; OnPropertyChanged("SaveButtonEnabled"); } }
        public bool ResetButtonEnabled { get { return this._resetButtonEnabled; } set { this._resetButtonEnabled = value; OnPropertyChanged("ResetButtonEnabled"); } }
        public bool SimulateButtonEnabled { get { return this._simulateButtonEnabled; } set { this._simulateButtonEnabled = value; OnPropertyChanged("SimulateButtonEnabled"); } }

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

        private bool _loadButtonEnabled;
        private bool _saveButtonEnabled;
        private bool _resetButtonEnabled;
        private bool _simulateButtonEnabled;

        public DrawWindow(ImageSourceWrapper imageSourceWrapper, int desiredDrawAreaWidth, int desiredDrawAreaHeight)
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

            this.LoadButtonEnabled = false; // Temporarily disabled
            this.SaveButtonEnabled = false; // Temporarily disabled
            this.ResetButtonEnabled = true;
            this.SimulateButtonEnabled = true;

            InitializeComponent();
            this.ImageSourceWrapper.Source.Changed += (sender, e) => { this.DrawImageContainer.InvalidateVisual(); };
        }

        // When the window is resized by the user, the desired size of the draw area and size of button menu is programmatically set based on
        // proportions given as a window resource 'buttonMenuHeightFactor'.
        private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DrawAreaSize.NewSize((int)e.NewSize.Width, (int)(e.NewSize.Height / (1 + (double)this.Resources["buttonMenuHeightToDrawAreaFactor"])));
            OnPropertyChanged("DrawAreaSize");
        }

        private void DrawImage_MouseWheel(object sender, MouseWheelEventArgs e)
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

            double centerLocalX = e.GetPosition(this.DrawImageContainer).X / this.DrawAreaSize.Width;
            double centerLocalY = e.GetPosition(this.DrawImageContainer).Y / this.DrawAreaSize.Height;

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

        private void DrawImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseDown(
                this._imageSourceWrapper.Source.Width * (this._screenOriginX + (this._screenEndX - this._screenOriginX) * e.GetPosition(this.DrawImageContainer).X / this.DrawAreaSize.Width),
                this._imageSourceWrapper.Source.Height * (this._screenOriginY + (this._screenEndY - this._screenOriginY) * e.GetPosition(this.DrawImageContainer).Y / this.DrawAreaSize.Height),
                e.ChangedButton);
        }

        private void DrawImage_MouseLeave(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseLeave(
                this._imageSourceWrapper.Source.Width * (this._screenOriginX + (this._screenEndX - this._screenOriginX) * e.GetPosition(this.DrawImageContainer).X / this.DrawAreaSize.Width),
                this._imageSourceWrapper.Source.Height * (this._screenOriginY + (this._screenEndY - this._screenOriginY) * e.GetPosition(this.DrawImageContainer).Y / this.DrawAreaSize.Height));
        }

        private void DrawImage_MouseEnter(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseEnter(
                this._imageSourceWrapper.Source.Width * (this._screenOriginX + (this._screenEndX - this._screenOriginX) * e.GetPosition(this.DrawImageContainer).X / this.DrawAreaSize.Width),
                this._imageSourceWrapper.Source.Height * (this._screenOriginY + (this._screenEndY - this._screenOriginY) * e.GetPosition(this.DrawImageContainer).Y / this.DrawAreaSize.Height));
        }

        private void DrawImage_MouseMove(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseMove(
                this._imageSourceWrapper.Source.Width * (this._screenOriginX + (this._screenEndX - this._screenOriginX) * e.GetPosition(this.DrawImageContainer).X / this.DrawAreaSize.Width),
                this._imageSourceWrapper.Source.Height * (this._screenOriginY + (this._screenEndY - this._screenOriginY) * e.GetPosition(this.DrawImageContainer).Y / this.DrawAreaSize.Height));
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainApp)Application.Current).Load();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainApp)Application.Current).Save();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainApp)Application.Current).Reset();
        }

        private void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainApp)Application.Current).Simulate();
        }

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

