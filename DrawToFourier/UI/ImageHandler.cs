using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawToFourier.UI
{
    internal class ImageHandler : ImageBinding
    {
        public ImageHandler()
        {
            int length = Math.Min((int)(SystemParameters.PrimaryScreenWidth * 0.5), (int)(SystemParameters.PrimaryScreenHeight * 0.5));
            this.ImageWidth = length;
            this.ImageHeight = length;
            this.Image = new WriteableBitmap(this.ImageWidth, this.ImageHeight, 96, 96, PixelFormats.Bgr32, null);
        }

        public override void OnImageNewSizeRequest(int width, int height)
        {
            //System.Diagnostics.Debug.WriteLine($"{width} {height}");
            this.ImageWidth = width;
            this.ImageHeight = height;
        }
    }
}
