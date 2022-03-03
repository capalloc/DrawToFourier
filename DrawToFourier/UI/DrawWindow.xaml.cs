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

        public bool LoadButtonEnabled { get { return this._loadButtonEnabled; } set { this._loadButtonEnabled = value; OnPropertyChanged("LoadButtonEnabled"); } }
        public bool SaveButtonEnabled { get { return this._saveButtonEnabled; } set { this._saveButtonEnabled = value; OnPropertyChanged("SaveButtonEnabled"); } }
        public bool ResetButtonEnabled { get { return this._resetButtonEnabled; } set { this._resetButtonEnabled = value; OnPropertyChanged("ResetButtonEnabled"); } }
        public bool SimulateButtonEnabled { get { return this._simulateButtonEnabled; } set { this._simulateButtonEnabled = value; OnPropertyChanged("SimulateButtonEnabled"); } }

        private ImageSourceWrapper _imageSourceWrapper;
        private DrawAreaSize _drawAreaSize;

        private bool _loadButtonEnabled;
        private bool _saveButtonEnabled;
        private bool _resetButtonEnabled;
        private bool _simulateButtonEnabled;

        // Scaleback variables are used to translate draw area coordinates to image coordinates
        private double xScaleBack; 
        private double yScaleBack;


        public DrawWindow(ImageSourceWrapper imageSourceWrapper, int desiredDrawAreaWidth, int desiredDrawAreaHeight)
        {
            this.ImageSourceWrapper = imageSourceWrapper;
            this.DrawAreaSize = new DrawAreaSize(desiredDrawAreaWidth, desiredDrawAreaHeight);
            this.xScaleBack = this.ImageSourceWrapper.Source.Width / this.DrawAreaSize.Width;
            this.yScaleBack = this.ImageSourceWrapper.Source.Height / this.DrawAreaSize.Height;
            this.LoadButtonEnabled = false; // Temporarily disabled
            this.SaveButtonEnabled = false; // Temporarily disabled
            this.ResetButtonEnabled = true;
            this.SimulateButtonEnabled = true;
            InitializeComponent();
        }

        // When the window is resized by the user, the desired size of the draw area and size of button menu is programmatically set based on
        // proportions given as a window resource 'buttonMenuHeightFactor'.
        private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DrawAreaSize.NewSize((int)e.NewSize.Width, (int)(e.NewSize.Height / (1 + (double)this.Resources["buttonMenuHeightToDrawAreaFactor"])));
            this.xScaleBack = this.ImageSourceWrapper.Source.Width / this.DrawAreaSize.Width;
            this.yScaleBack = this.ImageSourceWrapper.Source.Height / this.DrawAreaSize.Height;
            OnPropertyChanged("DrawAreaSize");
        }

        private void DrawImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseDown(e.GetPosition(this.DrawImageContainer).X * xScaleBack, e.GetPosition(this.DrawImageContainer).Y * yScaleBack, e.ChangedButton);
        }

        private void DrawImage_MouseLeave(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseLeave(e.GetPosition(this.DrawImageContainer).X * xScaleBack, e.GetPosition(this.DrawImageContainer).Y * yScaleBack);
        }

        private void DrawImage_MouseEnter(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseEnter(e.GetPosition(this.DrawImageContainer).X * xScaleBack, e.GetPosition(this.DrawImageContainer).Y * yScaleBack);
        }

        private void DrawImage_MouseMove(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseMove(e.GetPosition(this.DrawImageContainer).X * xScaleBack, e.GetPosition(this.DrawImageContainer).Y * yScaleBack);
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

