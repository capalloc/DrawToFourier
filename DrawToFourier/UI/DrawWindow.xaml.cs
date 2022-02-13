using System;
using System.Collections.Generic;
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
    public partial class DrawWindow : Window
    {
        public ImageBinding ImageContext { get; set; }

        public DrawWindow(ImageBinding imageBinding)
        {
            this.ImageContext = imageBinding;
            InitializeComponent();
        }

        private void MainContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"{(int)e.NewSize.Width} {(int)e.NewSize.Height}");
            this.ImageContext.OnImageNewSizeRequest((int) e.NewSize.Width, (int) (e.NewSize.Height / (1 + (double) this.Resources["buttonMenuHeightFactor"])));
        }
    }
}

