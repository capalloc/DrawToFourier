﻿using System;
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

namespace DrawToFourier.UI
{
    public partial class DrawWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSourceWrapper ImageWrapper { get { return this._imageWrapper; } }
        public int DesiredDrawAreaWidth { get { return this._desiredDrawAreaWidth; } set { this._desiredDrawAreaWidth = value; OnPropertyChanged("DesiredDrawAreaWidth"); } }
        public int DesiredDrawAreaHeight { get { return this._desiredDrawAreaHeight; } set { this._desiredDrawAreaHeight = value; OnPropertyChanged("DesiredDrawAreaHeight"); } }

        private ImageSourceWrapper _imageWrapper;
        private int _desiredDrawAreaWidth;
        private int _desiredDrawAreaHeight;
        private double xScaleBack;
        private double yScaleBack;

        public DrawWindow(ImageSourceWrapper imageWrapper)
        {
            this._imageWrapper = imageWrapper;
            this.DesiredDrawAreaWidth = (int) imageWrapper.Source.Width;
            this.DesiredDrawAreaHeight = (int) imageWrapper.Source.Height;
            this.xScaleBack = 1;
            this.yScaleBack = 1;
            InitializeComponent();
        }

        private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.DesiredDrawAreaWidth = (int)e.NewSize.Width;
            this.DesiredDrawAreaHeight = (int)(e.NewSize.Height / (1 + (double)this.Resources["buttonMenuHeightFactor"]));
            this.xScaleBack = this.ImageWrapper.Source.Width / this.DrawImage.ActualWidth;
            this.yScaleBack = this.ImageWrapper.Source.Height / this.DrawImage.ActualHeight;
        }

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void DrawImage_MouseDown(object sender, MouseButtonEventArgs e)
        {

            this._imageWrapper.OnMouseDown(e.GetPosition(this.DrawImage).X * xScaleBack, e.GetPosition(this.DrawImage).Y * yScaleBack);
        }

        private void DrawImage_MouseLeave(object sender, MouseEventArgs e)
        {
            this._imageWrapper.OnMouseLeave(e.GetPosition(this.DrawImage).X * xScaleBack, e.GetPosition(this.DrawImage).Y * yScaleBack);
        }

        private void DrawImage_MouseEnter(object sender, MouseEventArgs e)
        {
            this._imageWrapper.OnMouseEnter(e.GetPosition(this.DrawImage).X * xScaleBack, e.GetPosition(this.DrawImage).Y * yScaleBack);
        }

        private void DrawImage_MouseMove(object sender, MouseEventArgs e)
        {
            this._imageWrapper.OnMouseMove(e.GetPosition(this.DrawImage).X * xScaleBack, e.GetPosition(this.DrawImage).Y * yScaleBack);
        }

    }
}

