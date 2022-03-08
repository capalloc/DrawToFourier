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
        private uint[][] _layerBuffers; // Layer 0 is the composed buffer which is fed to the backbuffer
        private uint _brushColor;
        private int _activeLayer; // Null for directly writing to composed buffer
        private PriorityQueue<Action, int> _deferredJobs;

        public ImageHandler(int width, int height, int maxLayerCount)
        {
            this.Source = this._bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            this._layerBuffers = new uint[maxLayerCount][];
            this._layerBuffers[0] = new uint[this._bmp.PixelWidth * this._bmp.PixelHeight];
            this._activeLayer = 0;
            this._deferredJobs = new PriorityQueue<Action, int>();
            this._brushColor = 0xFFFFFFFF;

            for (int i = 0; i < this._layerBuffers[0].Length; i++)
                this._layerBuffers[0][i] = 0xFF000000;
        }

        public void Reset()
        {
            this._layerBuffers = new uint[this._layerBuffers.Length][];
            this._layerBuffers[0] = new uint[this._bmp.PixelWidth * this._bmp.PixelHeight];
            this._activeLayer = 0;
            this._deferredJobs = new PriorityQueue<Action, int>();
            this._brushColor = 0xFFFFFFFF;

            for (int i = 0; i < this._layerBuffers[0].Length; i++)
                this._layerBuffers[0][i] = 0xFF000000;
        }

        public void Update()
        {
            this._bmp.WritePixels(new Int32Rect(0, 0, this._bmp.PixelWidth, this._bmp.PixelHeight), this._layerBuffers[0], 4 * this._bmp.PixelWidth, 0);
        }

        public void Compose()
        {
            int length = this._bmp.PixelWidth * this._bmp.PixelHeight;

            for (int i = 0; i < length; i++)
            {
                uint aB = 0;
                uint rB = 0;
                uint gB = 0;
                uint bB = 0;
                 
                for (int l = 0; l < this._layerBuffers.Length; l++)
                {
                    if (this._layerBuffers[l] == null) continue;

                    uint aA = (byte)((this._layerBuffers[l][i] & 0xFF000000) >> 24);
                    uint rA = (byte)((this._layerBuffers[l][i] & 0x00FF0000) >> 16);
                    uint gA = (byte)((this._layerBuffers[l][i] & 0x0000FF00) >> 8);
                    uint bA = (byte)((this._layerBuffers[l][i] & 0x000000FF));

                    uint aO = (255 * aA + 255 * aB - aA * aB) / 255;

                    if (aO == 0)
                    {
                        rB = 0;
                        gB = 0;
                        bB = 0;
                    } 
                    else
                    {
                        rB = (255 * rA * aA + 255 * rB * aB - rB * aB * aA) / (255 * aO);
                        gB = (255 * gA * aA + 255 * gB * aB - gB * aB * aA) / (255 * aO);
                        bB = (255 * bA * aA + 255 * bB * aB - bB * aB * aA) / (255 * aO);
                    }
                    aB = aO;
                }

                this._layerBuffers[0][i] = (rB << 24) | (rB << 16) | (gB << 8) | bB;
            }
        }

        public void ApplyDeferredJobs()
        {
            uint oldBrushColor = this._brushColor;
            int oldLayer = this._activeLayer;

            while (this._deferredJobs.Count > 0)
                this._deferredJobs.Dequeue().Invoke();

            this._brushColor = oldBrushColor;
            this._activeLayer = oldLayer;
        }

        public void ChangeBrushColor(int r, int g, int b)
        {
            this._brushColor = 0xFF000000 | (uint)((r << 16) | (g << 8) | b);
        }

        public void ChangeActiveLayer(int newActiveLayer)
        {
            this._activeLayer = newActiveLayer;

            if (this._layerBuffers[newActiveLayer] == null)
                this._layerBuffers[newActiveLayer] = new uint[this._bmp.PixelWidth * this._bmp.PixelHeight];
        }

        public void ClearAll()
        {
            for (int i = 0; i < this._layerBuffers[0].Length; i++)
                this._layerBuffers[0][i] = 0xFF000000;

            for (int i = 1; i < this._layerBuffers.Length; i++)
            {
                if (this._layerBuffers[i] == null) continue;

                this._layerBuffers[i] = new uint[this._bmp.PixelWidth * this._bmp.PixelHeight];
            }
        }

        public void Clear()
        {
            if (this._activeLayer != 0)
                this._layerBuffers[this._activeLayer] = new uint[this._bmp.PixelWidth * this._bmp.PixelHeight];
            else
            {
                for (int i = 0; i < this._layerBuffers[0].Length; i++)
                    this._layerBuffers[0][i] = 0xFF000000;
            }
        }

        public void DrawHollowCircle(Point circleCenter, int diameter, int brushSize)
        {
            double radius =  diameter / 2;
            double unitAngle =  1 / radius;

            Point prevPoint = new Point(circleCenter.X + radius, circleCenter.Y);

            for (double t = unitAngle; t < 2 * Math.PI; t += unitAngle)
            {
                Point p = new Point(circleCenter.X + radius * Math.Cos(t), circleCenter.Y + radius * Math.Sin(t));
                DrawLine(p, prevPoint, brushSize);
                prevPoint = p;
            }
        }

        // Draws a circle at given poit with given diameter
        public void DrawSolidCircle(Point circleCenter, int diameter)
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
                        this._layerBuffers[this._activeLayer][(i + rY) * this._bmp.PixelWidth + rX + j] = this._brushColor;
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
                        this._layerBuffers[this._activeLayer][(i + rY) * this._bmp.PixelWidth + rX + j] = this._brushColor;
                }
            }
        }

        // Draws a line with 3 pixel stroke on current bitmap between given points.
        // It does this by linearly interpolating points between given input points and draws a dot on each of them.
        // Returns the last drawn point (right now return value may not be correct)
        public void DrawLine(Point p1, Point p2, int brushSize)
        {
            Vector pD = p2 - p1;

            if (Math.Abs(pD.X) >= Math.Abs(pD.Y))
            {
                if (p1.X < p2.X)
                {

                    for (int x = (int)p1.X; x <= (int)p2.X; x++)
                    {
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        DrawSolidCircle(targetP, brushSize);
                    }
                }
                else
                {
                    for (int x = (int)p2.X; x <= (int)p1.X; x++)
                    {
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        DrawSolidCircle(targetP, brushSize);
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
                        DrawSolidCircle(targetP, brushSize);
                    }
                }
                else
                {
                    for (int y = (int)p2.Y; y <= (int)p1.Y; y++)
                    {
                        Point targetP = new Point(Linear(y, tp1, tp2), (double)y);
                        DrawSolidCircle(targetP, brushSize);
                    }
                }
            }
        }

        public void DeferredDrawHollowCircle(int priority, Point circleCenter, int diameter, int brushSize)
        {
            uint oldBrushColor = this._brushColor;
            int oldLayer = this._activeLayer;
            this._deferredJobs.Enqueue( new Action(() => {
                this._brushColor = oldBrushColor;
                this._activeLayer = oldLayer;
                DrawHollowCircle(circleCenter, diameter, brushSize); 
            }), priority);
        }

        public void DeferredDrawSolidCircle(int priority, Point circleCenter, int diameter)
        {
            uint oldBrushColor = this._brushColor;
            int oldLayer = this._activeLayer;
            this._deferredJobs.Enqueue(new Action(() => {
                this._brushColor = oldBrushColor;
                this._activeLayer = oldLayer;
                DrawSolidCircle(circleCenter, diameter);
            }), priority);
        }

        public void DeferredDrawLine(int priority, Point p1, Point p2, int brushSize)
        {
            uint oldBrushColor = this._brushColor;
            int oldLayer = this._activeLayer;
            this._deferredJobs.Enqueue(new Action(() => {
                this._brushColor = oldBrushColor;
                this._activeLayer = oldLayer;
                DrawLine(p1, p2, brushSize); 
            }), priority);
        }
    }
}