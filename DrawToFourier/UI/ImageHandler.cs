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

    public class ImageHandler : ImageSourceWrapper
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
        private uint[] _buffer;
        private LinkedList<Action>[] _layers;
        private bool[] _layerModes; // False = LIFO, True = FIFO when composing

        public ImageHandler(int width, int height, int maxLayerCount = 0)
        {
            this.Source = this._bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            this._buffer = new uint[this._bmp.PixelWidth * this._bmp.PixelHeight];
            Array.Fill(this._buffer, 0xFF000000);
            this._layers = new LinkedList<Action>[maxLayerCount];
            this._layerModes = new bool[maxLayerCount];

            for (int i = 0; i < maxLayerCount; i++)
                this._layers[i] = new LinkedList<Action>();
        }

        public void RenderBuffer()
        {
            this._bmp.WritePixels(new Int32Rect(0, 0, this._bmp.PixelWidth, this._bmp.PixelHeight), this._buffer, 4 * this._bmp.PixelWidth, 0);
        }

        public void ClearBuffer()
        {
            Array.Fill(this._buffer, 0xFF000000);
        }

        public void ClearLayer(int layer)
        {
            this._layers[layer].Clear();
        }

        public void SetLayerMode(int layer, bool mode)
        {
            this._layerModes[layer] = mode;
        }

        // Draws a hollow circle at given point with given diameter
        public void DrawHollowCircle(Point circleCenter, int diameter, int brushSize, byte r, byte g, byte b, byte a = byte.MaxValue, int? layer = null)
        {
            uint color = this._brushColorToUint(r, g, b, a);

            if (layer == null)
                this._drawHollowCircleToBuffer(circleCenter, diameter, brushSize, color); 
            else
            {
                if (this._layerModes[(int)layer]) // FIFO
                    this._layers[(int)layer].AddFirst(() => { this._drawHollowCircleToBuffer(circleCenter, diameter, brushSize, color); });
                else // LIFO
                    this._layers[(int)layer].AddLast(() => { this._drawHollowCircleToBuffer(circleCenter, diameter, brushSize, color); });
            }
        }

        // Draws a filled circle at given point with given diameter
        public void DrawSolidCircle(Point circleCenter, int diameter, byte r, byte g, byte b, byte a = byte.MaxValue, int? layer = null)
        {
            uint color = this._brushColorToUint(r, g, b, a);
            
            if (layer == null)
                this._drawSolidCircleToBuffer(circleCenter, diameter, color);
            else
            {
                if (this._layerModes[(int)layer]) // FIFO
                    this._layers[(int)layer].AddFirst(() => { this._drawSolidCircleToBuffer(circleCenter, diameter, color); });
                else // LIFO
                    this._layers[(int)layer].AddLast(() => { this._drawSolidCircleToBuffer(circleCenter, diameter, color); });
            }
        }

        // Draws a line with 3 pixel stroke on current bitmap between given points.
        // It does this by linearly interpolating points between given input points and draws a dot on each of them.
        // Returns the last drawn point (right now return value may not be correct)
        public void DrawLine(Point p1, Point p2, int brushSize, byte r, byte g, byte b, byte a = byte.MaxValue, int? layer = null)
        {
            uint color = this._brushColorToUint(r, g, b, a);
            
            if (layer == null)
                this._drawLineToBuffer(p1, p2, brushSize, color);
            else
            {
                if (this._layerModes[(int)layer]) // FIFO
                    this._layers[(int)layer].AddFirst(() => { this._drawLineToBuffer(p1, p2, brushSize, color); });
                else // LIFO
                    this._layers[(int)layer].AddLast(() => { this._drawLineToBuffer(p1, p2, brushSize, color); });
            }
        }

        private uint _brushColorToUint(byte r, byte g, byte b, byte a)
        {
            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }

        private uint _compositeColor(uint backgroundColor, uint addedColor)
        {
            uint aB = (byte)((backgroundColor & 0xFF000000) >> 24);
            uint aA = (byte)((addedColor & 0xFF000000) >> 24);

            if (aA == byte.MaxValue || aB == 0)
                return addedColor;
            if (aA == 0)
                return backgroundColor;

            uint aO = (255 * aA + 255 * aB - aA * aB) / 255;

            if (aO == 0)
                return 0;

            uint rB = (byte)((backgroundColor & 0x00FF0000) >> 16);
            uint gB = (byte)((backgroundColor & 0x0000FF00) >> 8);
            uint bB = (byte)((backgroundColor & 0x000000FF));

            uint rA = (byte)((addedColor & 0x00FF0000) >> 16);
            uint gA = (byte)((addedColor & 0x0000FF00) >> 8);
            uint bA = (byte)((addedColor & 0x000000FF));

            rB = (255 * rA * aA + 255 * rB * aB - rB * aB * aA) / (255 * aO);
            gB = (255 * gA * aA + 255 * gB * aB - gB * aB * aA) / (255 * aO);
            bB = (255 * bA * aA + 255 * bB * aB - bB * aB * aA) / (255 * aO);

            return (aO << 24) | (rB << 16) | (gB << 8) | bB;
        }

        // Buffer writing functions

        private void _drawHollowCircleToBuffer(Point circleCenter, int diameter, int brushSize, uint color)
        {
            double radius = diameter / 2;
            double unitAngle = 1 / radius;

            Point prevPoint = new Point(circleCenter.X + radius, circleCenter.Y);

            for (double t = unitAngle; t < 2 * Math.PI; t += unitAngle)
            {
                Point p = new Point(circleCenter.X + radius * Math.Cos(t), circleCenter.Y + radius * Math.Sin(t));
                this._drawLineToBuffer(p, prevPoint, brushSize, color);
                prevPoint = p;
            }
        }

        private void _drawSolidCircleToBuffer(Point circleCenter, int diameter, uint color)
        {
            int w, h;
            h = w = diameter;
            double cX, cY; // Adjusted circle center

            if (diameter % 2 == 0)  // If diameter is even
            {
                cX = Math.Round(circleCenter.X - 0.5) + 0.5;
                cY = Math.Round(circleCenter.Y - 0.5) + 0.5;
            }
            else  // If diameter is odd
            {
                cX = Math.Round(circleCenter.X);
                cY = Math.Round(circleCenter.Y);
            }

            int rX = (int)(cX - diameter / 2.0 + 0.5);
            int rY = (int)(cY - diameter / 2.0 + 0.5);

            if (rX < 0)
            {
                w -= -rX;
                rX = 0;
            }
            if (rY < 0)
            {
                h -= -rY;
                rY = 0;
            }

            if (h <= 0 || w <= 0) // If circle is completely outside the bounds
                return;

            if (rX + w > this._bmp.PixelWidth)
            {
                w = this._bmp.PixelWidth - rX;
            }
            if (rY + h > this._bmp.PixelHeight)
            {
                h = this._bmp.PixelHeight - rY;
            }

            if (h <= 0 || w <= 0) // If circle is completely outside the bounds
                return;

            if (diameter % 2 == 0)  // If diameter is even
            {
                for (int i = 0; i < h; i++)
                {
                    double relY = rY - cY + i;
                    double startX = Math.Round(-Math.Sqrt(Math.Pow(diameter / 2.0, 2) - Math.Pow(relY, 2)) - double.Epsilon) + 0.5;
                    double endX = -Math.Round(-Math.Sqrt(Math.Pow(diameter / 2.0, 2) - Math.Pow(relY, 2)) - double.Epsilon) - 0.5;

                    int jStart = Math.Max((int)(cX - rX + startX), 0);
                    int jEnd = Math.Min((int)(cX - rX + endX), w - 1);

                    for (int j = jStart; j <= jEnd; j++)
                    {
                        int pixelIndex = (i + rY) * this._bmp.PixelWidth + rX + j;
                        this._buffer[pixelIndex] = this._compositeColor(this._buffer[pixelIndex], color);
                    }
                        
                }
            }
            else  // If diameter is odd
            {
                for (int i = 0; i < h; i++)
                {
                    double relY = rY - cY + i;
                    double startX = Math.Round(-Math.Sqrt(Math.Pow(diameter / 2.0, 2) - Math.Pow(relY, 2)) + 0.5 - double.Epsilon);
                    double endX = -Math.Round(-Math.Sqrt(Math.Pow(diameter / 2.0, 2) - Math.Pow(relY, 2)) + 0.5 - double.Epsilon);

                    int jStart = Math.Max((int)(cX - rX + startX), 0);
                    int jEnd = Math.Min((int)(cX - rX + endX), w - 1);

                    for (int j = jStart; j <= jEnd; j++)
                    {
                        int pixelIndex = (i + rY) * this._bmp.PixelWidth + rX + j;
                        this._buffer[pixelIndex] = this._compositeColor(this._buffer[pixelIndex], color);
                    }
                }
            }
        }

        private void _drawLineToBuffer(Point p1, Point p2, int brushSize, uint color)
        {
            Vector pD = p2 - p1;

            if (Math.Abs(pD.X) >= Math.Abs(pD.Y))
            {
                if (p1.X < p2.X)
                {

                    for (int x = (int)p1.X; x <= (int)p2.X; x++)
                    {
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        this._drawSolidCircleToBuffer(targetP, brushSize, color);
                    }
                }
                else
                {
                    for (int x = (int)p2.X; x <= (int)p1.X; x++)
                    {
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        this._drawSolidCircleToBuffer(targetP, brushSize, color);
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
                        this._drawSolidCircleToBuffer(targetP, brushSize, color);
                    }
                }
                else
                {
                    for (int y = (int)p2.Y; y <= (int)p1.Y; y++)
                    {
                        Point targetP = new Point(Linear(y, tp1, tp2), (double)y);
                        this._drawSolidCircleToBuffer(targetP, brushSize, color);
                    }
                }
            }
        }

    }
}