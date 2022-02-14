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
        public static readonly Int32Rect dotRect = new Int32Rect(0, 0, 3, 3);

        public static readonly uint[] dotColorArray = {
                0xFFFFFFFF,0x00FFFFFF,0xFFFFFFFF,
                0x00FFFFFF,0x00FFFFFF,0x00FFFFFF,
                0xFFFFFFFF,0x00FFFFFF,0xFFFFFFFF
            };

        public static bool DrawDot(WriteableBitmap bmp, Point dotCenter)
        {
            Int32Rect rect = dotRect;
            uint[] colorArray = new uint[9];

            rect.X = (int)dotCenter.X - 1;
            rect.Y = (int)dotCenter.Y - 1;

            if ((int)dotCenter.X > 0 && (int)dotCenter.X < bmp.Width - 1 && (int)dotCenter.Y > 0 && (int)dotCenter.Y < bmp.Height - 1)
            {
                bmp.CopyPixels(rect, colorArray, 12, 0);

                for (int i = 0; i < dotColorArray.Length; i++)
                {
                    if (dotColorArray[i] != 0xFFFFFFFF)
                        colorArray[i] = dotColorArray[i];
                }

                bmp.WritePixels(dotRect, colorArray, 12, (int)dotCenter.X - 1, (int)dotCenter.Y - 1);

                return true;
            }
            else
            {
                return false;
            }

        }

        public static Point DrawLine(WriteableBitmap bmp, Point p1, Point p2)
        {
            Vector pD = p2 - p1;
            Point lastTarget = p1;

            if (Math.Abs(pD.X) >= Math.Abs(pD.Y))
            {
                if (p1.X < p2.X)
                {

                    for (int x = (int)p1.X; x <= (int)p2.X; x++)
                    {
                        Point targetP = new Point((double)x, linear(x, p1, p2));
                        if (!DrawDot(bmp, targetP))
                        {
                            return lastTarget;
                        }
                        lastTarget = targetP;
                    }
                }
                else
                {
                    for (int x = (int)p2.X; x <= (int)p1.X; x++)
                    {
                        Point targetP = new Point((double)x, linear(x, p1, p2));
                        if (!DrawDot(bmp, targetP))
                        {
                            return lastTarget;
                        }
                        lastTarget = targetP;
                    }
                }
            }
            else
            {
                Point tp1 = new Point(p1.Y, p1.X);
                Point tp2 = new Point(p2.Y, p2.X);

                if (p1.Y < p2.Y)
                {
                    for (int y = (int)p1.Y; y <= (int)p2.Y; y++)
                    {
                        Point targetP = new Point(linear(y, tp1, tp2), (double)y);
                        if (!DrawDot(bmp, targetP))
                        {
                            return lastTarget;
                        }
                        lastTarget = targetP;
                    }
                }
                else
                {
                    for (int y = (int)p2.Y; y <= (int)p1.Y; y++)
                    {
                        Point targetP = new Point(linear(y, tp1, tp2), (double)y);
                        if (!DrawDot(bmp, targetP))
                        {
                            return lastTarget;
                        }
                        lastTarget = targetP;
                    }
                }
            }

            return lastTarget;
        }

        public static double linear(double x, Point p1, Point p2)
        {
            if ((p2.X - p1.X) == 0)
            {
                return (p1.Y + p2.Y) / 2;
            }
            return p1.Y + (x - p1.X) * (p2.Y - p1.Y) / (p2.X - p1.X);
        }

        private WriteableBitmap _bmp;
        public ImageHandler()
        {
            int length = Math.Min((int)(SystemParameters.PrimaryScreenWidth * 0.5), (int)(SystemParameters.PrimaryScreenHeight * 0.5));
            this.NewSizeRequest(length, length);
            this.ImageSource = this._bmp = new WriteableBitmap(length, length, 96, 96, PixelFormats.Bgr32, null);
            DrawLine(this._bmp, new Point(1,1), new Point(length-1,length-1));
        }
    }
}
