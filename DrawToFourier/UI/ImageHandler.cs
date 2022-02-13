using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawToFourier.UI
{
    internal class ImageHandler : ImageBinding
    {
        public static readonly int initialCanvasWidth = 800;
        public static readonly int initialCanvasHeight = 800;

        public ImageHandler()
        {
            this.ImageWidth = initialCanvasWidth;
            this.ImageHeight = initialCanvasHeight;
            this.Image = new WriteableBitmap(this.ImageWidth, this.ImageHeight, 96, 96, PixelFormats.Bgr32, null);
        }
        
    }
}
