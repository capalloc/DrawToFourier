﻿using DrawToFourier.Fourier;
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

namespace DrawToFourier.UI
{
    public partial class DrawWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSourceWrapper ImageWrapper { 
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

        public bool LoadButtonEnabled { get { return this._loadButtonEnabled; } set { this._loadButtonEnabled = value; OnPropertyChanged("LoadButtonEnabled"); } }
        public bool SaveButtonEnabled { get { return this._saveButtonEnabled; } set { this._saveButtonEnabled = value; OnPropertyChanged("SaveButtonEnabled"); } }
        public bool ResetButtonEnabled { get { return this._resetButtonEnabled; } set { this._resetButtonEnabled = value; OnPropertyChanged("ResetButtonEnabled"); } }
        public bool SimulateButtonEnabled { get { return this._simulateButtonEnabled; } set { this._simulateButtonEnabled = value; OnPropertyChanged("SimulateButtonEnabled"); } }

        private ImageSourceWrapper _imageWrapper;
        private int _desiredDrawAreaWidth;
        private int _desiredDrawAreaHeight;

        private bool _loadButtonEnabled;
        private bool _saveButtonEnabled;
        private bool _resetButtonEnabled;
        private bool _simulateButtonEnabled;

        // Scaleback variables are used to translate draw area coordinates to image coordinates
        private double xScaleBack; 
        private double yScaleBack;

        public DrawWindow(ImageSourceWrapper imageWrapper)
        {
            this._imageWrapper = imageWrapper;
            this.DesiredDrawAreaWidth = (int) imageWrapper.Source.Width;
            this.DesiredDrawAreaHeight = (int) imageWrapper.Source.Height;
            this.LoadButtonEnabled = false; // Temporarily disabled
            this.SaveButtonEnabled = false; // Temporarily disabled
            this.ResetButtonEnabled = true;
            this.SimulateButtonEnabled = true;
            this.xScaleBack = 1;
            this.yScaleBack = 1;
            InitializeComponent();
        }

        // When the window is resized by the user, the desired size of the draw area and size of button menu is programmatically set based on
        // proportions given as a window resource 'buttonMenuHeightFactor'.
        private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DesiredDrawAreaWidth = (int)e.NewSize.Width;
            this.DesiredDrawAreaHeight = (int)(e.NewSize.Height / (1 + (double)this.Resources["buttonMenuHeightFactor"]));
            // Scaleback variables are set asynchronously with a low priority because to calculate them correctly, render thread must update first
            this.Dispatcher.InvokeAsync(() => {
                this.xScaleBack = this.ImageWrapper.Source.Width / this.DrawImage.ActualWidth;
                this.yScaleBack = this.ImageWrapper.Source.Height / this.DrawImage.ActualHeight;
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void DrawImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseDown(e.GetPosition(this.DrawImage).X * xScaleBack, e.GetPosition(this.DrawImage).Y * yScaleBack, e.ChangedButton);
        }

        private void DrawImage_MouseLeave(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseLeave(e.GetPosition(this.DrawImage).X * xScaleBack, e.GetPosition(this.DrawImage).Y * yScaleBack);
        }

        private void DrawImage_MouseEnter(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseEnter(e.GetPosition(this.DrawImage).X * xScaleBack, e.GetPosition(this.DrawImage).Y * yScaleBack);
        }

        private void DrawImage_MouseMove(object sender, MouseEventArgs e)
        {
            ((MainApp)Application.Current).OnMouseMove(e.GetPosition(this.DrawImage).X * xScaleBack, e.GetPosition(this.DrawImage).Y * yScaleBack);
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

