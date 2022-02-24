using DrawToFourier.Fourier;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawToFourier.UI
{
    internal class ImageHandler : ImageSourceWrapper
    {
        // Linear interpolation between two points
        public static double Linear(double x, Point p1, Point p2)
        {
            if ((p2.X - p1.X) == 0)
            {
                return (p1.Y + p2.Y) / 2;
            }
            return p1.Y + (x - p1.X) * (p2.Y - p1.Y) / (p2.X - p1.X);
        }

        private WriteableBitmap _bmp;

        public ImageHandler(int width, int height)
        {
            this.Source = this._bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
        }

        // Draws a white 3x3 square (called a dot) on current bitmap at given point. If no pixel is updated on the bitmap, returns false, otherwise returns true.
        public bool DrawDot(Point dotCenter)
        {
            Int32Rect rect = new Int32Rect(0, 0, 3, 3);
            int rectOriginX = 0;
            int rectOriginY = 0;

            if ((int)dotCenter.X > 0)
            {
                rectOriginX = (int)dotCenter.X - 1;

                if ((int)dotCenter.X > (int)this._bmp.Width)
                    return false;

                if ((int)dotCenter.X >= (int)this._bmp.Width - 1)
                    rect.Width = 1 + (int)this._bmp.Width - (int)dotCenter.X;
            }
            else if (dotCenter.X < -1)
            {
                return false;
            }
            else
            {
                rect.Width = 2 + (int)dotCenter.X;
            }

            if ((int)dotCenter.Y > 0)
            {
                rectOriginY = (int)dotCenter.Y - 1;

                if ((int)dotCenter.Y > (int)this._bmp.Height)
                    return false;

                if ((int)dotCenter.Y >= (int)this._bmp.Height - 1)
                    rect.Height = 1 + (int)this._bmp.Height - (int)dotCenter.Y;
            }
            else if (dotCenter.Y < -1)
            {
                return false;
            }
            else
            {
                rect.Height = 2 + (int)dotCenter.Y;
            }

            uint[] colorArray = new uint[rect.Width * rect.Height];

            for (int i = 0; i < colorArray.Length; i++)
                colorArray[i] = 0x00FFFFFF;

            this._bmp.WritePixels(rect, colorArray, rect.Width * 4, rectOriginX, rectOriginY);

            return true;
        }

        // Draws a line with 3 pixel stroke on current bitmap between given points.
        // It does this by linearly interpolating points between given input points and draws a dot on each of them.
        // Returns the last drawn point (right now return value may not be correct)
        public Point DrawLine(Point p1, Point p2)
        {
            Vector pD = p2 - p1;
            Point lastTarget = p1;

            if (Math.Abs(pD.X) >= Math.Abs(pD.Y))
            {
                if (p1.X < p2.X)
                {

                    for (int x = (int)p1.X; x <= (int)p2.X; x++)
                    {
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        if (!DrawDot(targetP))
                        {
                            //return lastTarget;
                        }
                        lastTarget = targetP;
                    }
                }
                else
                {
                    for (int x = (int)p2.X; x <= (int)p1.X; x++)
                    {
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        if (!DrawDot(targetP))
                        {
                            //return lastTarget;
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
                        Point targetP = new Point(Linear(y, tp1, tp2), (double)y);
                        if (!DrawDot(targetP))
                        {
                            //return lastTarget;
                        }
                        lastTarget = targetP;
                    }
                }
                else
                {
                    for (int y = (int)p2.Y; y <= (int)p1.Y; y++)
                    {
                        Point targetP = new Point(Linear(y, tp1, tp2), (double)y);
                        if (!DrawDot(targetP))
                        {
                            //return lastTarget;
                        }
                        lastTarget = targetP;
                    }
                }
            }

            return lastTarget;
        }

        public void DrawCircle(Point circleCenter, int diameter)
        {
            int w,h;
            h = w = diameter;
            uint[] arr = new uint[w * h];
            double cX, cY;

            if (diameter % 2 == 0)
            {
                cX = Math.Round(circleCenter.X - 0.5) + 0.5;
                cY = Math.Round(circleCenter.Y - 0.5) + 0.5;
            } 
            else
            {
                cX = Math.Round(circleCenter.X);
                cY = Math.Round(circleCenter.Y);
            }

            int rX = (int)(cX - diameter / 2.0 + 0.5);
            int rY = (int)(cY - diameter / 2.0 + 0.5);

            if (diameter % 2 == 0)
            {
                for (int i = 0; i < h; i++)
                {
                    double relY = rY - cY + i;
                    double startX = Math.Round(-Math.Sqrt(Math.Pow(diameter / 2.0, 2) - Math.Pow(relY, 2)) - double.Epsilon) + 0.5;

                    int jStart = (int)(cX - rX + startX);
                    int jEnd = (int)(cX - rX - startX);

                    for (int j = jStart; j <= jEnd; j++)
                        arr[w * i + j] = 0x00FFFFFF;
                }
            }
            else
            {
                for (int i = 0; i < h; i++)
                {
                    double relY = rY - cY + i;
                    double startX = Math.Round(-Math.Sqrt(Math.Pow(diameter / 2.0, 2) - Math.Pow(relY, 2)) + 0.5 - double.Epsilon);

                    int jStart = (int)(cX - rX + startX);
                    int jEnd = (int)(cX - rX - startX);

                    for (int j = jStart; j <= jEnd; j++)
                        arr[w * i + j] = 0x00FFFFFF;
                }
            }

            Int32Rect rect = new Int32Rect(0, 0, w, h);
            this._bmp.WritePixels(rect, arr, 4 * w, rX, rY);
        }
    }
}