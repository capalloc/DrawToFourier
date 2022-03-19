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

namespace CustomDrawingWithWPF
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
        private int _pixelCount;

        private uint[][] _layers;
        private uint[][] _layersComposed;

        private int[][] _layerChangedPixels;
        private int[][] _layerAllChangesPixels;
        private bool[][] _layerIsPixelChanged;
        private bool[][] _layerIsPixelAllChanges;
        private int[] _layerChangedPixelCount;
        private int[] _layerAllChangesPixelCount;

        private int?[] _nextLayerAfter;
        private int?[] _lastLayerBefore;

        public ImageHandler(int width, int height, int maxLayerCount = 1, byte bR = 0, byte bG = 0, byte bB = 0, byte bA = 255)
        {
            if (maxLayerCount < 1) throw new ArgumentOutOfRangeException(nameof(maxLayerCount));
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));

            this.Source = this._bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            this._pixelCount = this._bmp.PixelWidth * this._bmp.PixelHeight;

            this._layers = new uint[maxLayerCount + 1][];
            this._layersComposed = new uint[maxLayerCount + 1][];

            this._layerChangedPixels = new int[maxLayerCount + 1][];
            this._layerAllChangesPixels = new int[maxLayerCount + 1][];

            this._layerIsPixelChanged = new bool[maxLayerCount + 1][];
            this._layerIsPixelAllChanges = new bool[maxLayerCount + 1][];

            this._layers[0] = new uint[this._pixelCount];
            this._layersComposed[0] = this._layers[0];
            Array.Fill(this._layers[0], this._brushColorToUint(bR, bG, bB, bA));

            this._layers[1] = new uint[this._pixelCount];
            this._layersComposed[1] = new uint[this._pixelCount];
            Array.Copy(this._layersComposed[0], this._layersComposed[1], this._pixelCount);

            this._layerChangedPixels[1] = new int[this._pixelCount];
            this._layerAllChangesPixels[1] = new int[this._pixelCount];
            this._layerIsPixelChanged[1] = new bool[this._pixelCount];
            this._layerIsPixelAllChanges[1] = new bool[this._pixelCount];
            this._layerChangedPixelCount = new int[maxLayerCount + 1];
            this._layerAllChangesPixelCount = new int[maxLayerCount + 1];

            this._nextLayerAfter = new int?[maxLayerCount + 1];
            this._lastLayerBefore = new int?[maxLayerCount + 1];
            this._nextLayerAfter[0] = 1;
            this._lastLayerBefore[1] = 0;

            this._buffer = this._layersComposed[1];
        }

        public void RenderBuffer()
        {
            this._bmp.WritePixels(new Int32Rect(0, 0, this._bmp.PixelWidth, this._bmp.PixelHeight), this._buffer, 4 * this._bmp.PixelWidth, 0);
        }

        /*public void ClearBuffer()
        {
            Array.Fill(this._buffer, 0xFF000000);
        }*/

        public void ComposeLayers()
        {
            int currLayer = 1;
            int? prevLayer;
            int? nextLayer;

            do
            {
                prevLayer = this._lastLayerBefore[currLayer];
                nextLayer = this._nextLayerAfter[currLayer];

                if (this._layerChangedPixelCount[currLayer] > 0)
                {
                    if (nextLayer != null)
                    {
                        for (; this._layerChangedPixelCount[currLayer] > 0; this._layerChangedPixelCount[currLayer]--)
                        {
                            int pixelLoc = this._layerChangedPixels[currLayer][this._layerChangedPixelCount[currLayer] - 1];
                            uint newColor = this._compositeColor(this._layersComposed[(int)prevLayer!][pixelLoc], this._layers[currLayer][pixelLoc]);

                            if (this._layersComposed[currLayer][pixelLoc] == newColor) continue;
                            this._layersComposed[currLayer][pixelLoc] = newColor;

                            if (this._layers[(int)nextLayer][pixelLoc] >= 0xFF000000 || this._layerIsPixelChanged[(int)nextLayer][pixelLoc]) continue;

                            this._layerIsPixelChanged[(int)nextLayer][pixelLoc] = true;
                            this._layerChangedPixels[(int)nextLayer][this._layerChangedPixelCount[(int)nextLayer]] = pixelLoc;
                            this._layerChangedPixelCount[(int)nextLayer]++;
                        }
                    }
                    else
                    {
                        for (; this._layerChangedPixelCount[currLayer] > 0; this._layerChangedPixelCount[currLayer]--)
                        {
                            int pixelLoc = this._layerChangedPixels[currLayer][this._layerChangedPixelCount[currLayer] - 1];
                            uint newColor = this._compositeColor(this._layersComposed[(int)prevLayer!][pixelLoc], this._layers[currLayer][pixelLoc]);

                            if (this._layersComposed[currLayer][pixelLoc] == newColor) continue;
                            this._layersComposed[currLayer][pixelLoc] = newColor;
                        }
                    }

                    this._layerIsPixelChanged[currLayer] = new bool[this._pixelCount];
                }

                currLayer = nextLayer == null ? -1 : (int)nextLayer;
            } while (nextLayer != null);
        }
        
        /*private void _compose()
        {
            if (!this._layerIsPixelChanged[layer][pixel])
            {
                this._layerChangedPixels[layer][this._layerChangedPixelCount[layer]] = pixel;
                this._layerChangedPixelCount[layer]++;
                this._layerIsPixelChanged[layer][pixel] = true;
            }

            this._layers[layer][pixel] = color;
            int i = layer;

            while (true)
            {
                int? prevLayer = this._lastLayerBefore[i];

                if (prevLayer != null)
                {
                    uint newColor = this._compositeColor(this._layersComposed[(int)prevLayer!][pixel], this._layers[i][pixel]);

                    if (this._layersComposed[i][pixel] == newColor) break;

                    this._layersComposed[i][pixel] = newColor;
                }

                if (this._nextLayerAfter[i] == null) break;

                i = (int)this._nextLayerAfter[i]!;
            }
        }*/

        public void ClearLayer(int layer)
        {
            for (; this._layerAllChangesPixelCount[layer] > 0; this._layerAllChangesPixelCount[layer]--)
            {
                int pixelLoc = this._layerAllChangesPixels[layer][this._layerAllChangesPixelCount[layer] - 1];

                this._layers[layer][pixelLoc] = 0;

                if (!this._layerIsPixelChanged[layer][pixelLoc])
                {
                    this._layerChangedPixels[layer][this._layerChangedPixelCount[layer]] = pixelLoc;
                    this._layerChangedPixelCount[layer]++;
                    this._layerIsPixelChanged[layer][pixelLoc] = true;
                }

                this._layerIsPixelAllChanges[layer][pixelLoc] = false;
            }
        }

        /*public void ClearAllLayers()
        {
            for (int i = 0; i < this._layers.Length; i++)
                this._layers[i].Clear();
        }*/

        // Draws a hollow circle at given point with given diameter
        public void DrawHollowCircle(Point circleCenter, int diameter, int brushSize, byte r, byte g, byte b, byte a = 255, int layer = 1)
        {
            this._initializeLayerIfNotExist(layer);
            this._drawHollowCircle(circleCenter, diameter, brushSize, this._brushColorToUint(r, g, b, a), layer);
        }

        // Draws a filled circle at given point with given diameter
        public void DrawSolidCircle(Point circleCenter, int diameter, byte r, byte g, byte b, byte a = 255, int layer = 1)
        {
            this._initializeLayerIfNotExist(layer);
            this._drawSolidCircle(circleCenter, diameter, this._brushColorToUint(r, g, b, a), layer);
        }

        // Draws a line with 3 pixel stroke on current bitmap between given points.
        // It does this by linearly interpolating points between given input points and draws a dot on each of them.
        // Returns the last drawn point (right now return value may not be correct)
        public void DrawLine(Point p1, Point p2, int brushSize, byte r, byte g, byte b, byte a = 255, int layer = 1)
        {
            this._initializeLayerIfNotExist(layer);
            this._drawLine(p1, p2, brushSize, this._brushColorToUint(r, g, b, a), layer);
        }

        private void _initializeLayerIfNotExist(int layer)
        {
            if (layer < 1) throw new ArgumentOutOfRangeException(nameof(layer));

            if (this._layers[layer] == null)
            {
                this._layers[layer] = new uint[this._bmp.PixelWidth * this._bmp.PixelHeight];
                this._layersComposed[layer] = new uint[this._bmp.PixelWidth * this._bmp.PixelHeight];
                this._layerChangedPixels[layer] = new int[this._bmp.PixelWidth * this._bmp.PixelHeight];
                this._layerIsPixelChanged[layer] = new bool[this._bmp.PixelWidth * this._bmp.PixelHeight];
                this._layerAllChangesPixels[layer] = new int[this._bmp.PixelWidth * this._bmp.PixelHeight];
                this._layerIsPixelAllChanges[layer] = new bool[this._bmp.PixelWidth * this._bmp.PixelHeight];

                int i = layer - 1;

                while (this._layers[i] == null && i > 1) i--;

                this._nextLayerAfter[layer] = this._nextLayerAfter[i];

                if (this._nextLayerAfter[layer] != null)
                    this._lastLayerBefore[(int)this._nextLayerAfter[layer]!] = layer;
                else
                    this._buffer = this._layersComposed[layer];

                this._nextLayerAfter[i] = layer;
                this._lastLayerBefore[layer] = i;

                Array.Copy(this._layersComposed[i], this._layersComposed[layer], this._layersComposed[i].Length);
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

        private void _paintLayer(int layer, int pixel, uint color)
        {
            if (this._layers[layer][pixel] != color)
            {
                if (!this._layerIsPixelChanged[layer][pixel])
                {
                    this._layerChangedPixels[layer][this._layerChangedPixelCount[layer]] = pixel;
                    this._layerChangedPixelCount[layer]++;
                    this._layerIsPixelChanged[layer][pixel] = true;
                }

                if (!this._layerIsPixelAllChanges[layer][pixel])
                {
                    this._layerAllChangesPixels[layer][this._layerAllChangesPixelCount[layer]] = pixel;
                    this._layerAllChangesPixelCount[layer]++;
                    this._layerIsPixelAllChanges[layer][pixel] = true;
                }

                this._layers[layer][pixel] = color;
            }
        }

        // Buffer writing functions

        private void _drawHollowCircle(Point circleCenter, int diameter, int brushSize, uint color, int layer)
        {
            double radius = diameter / 2;
            double unitAngle = 1 / radius;

            Point prevPoint = new Point(circleCenter.X + radius, circleCenter.Y);

            for (double t = unitAngle; t < 2 * Math.PI; t += unitAngle)
            {
                Point p = new Point(circleCenter.X + radius * Math.Cos(t), circleCenter.Y + radius * Math.Sin(t));
                this._drawLine(p, prevPoint, brushSize, color, layer);
                prevPoint = p;
            }
        }

        private void _drawSolidCircle(Point circleCenter, int diameter, uint color, int layer)
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
                        this._paintLayer(layer, (i + rY) * this._bmp.PixelWidth + rX + j, color);
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
                        this._paintLayer(layer, (i + rY) * this._bmp.PixelWidth + rX + j, color);
                }
            }
        }

        private void _drawLine(Point p1, Point p2, int brushSize, uint color, int layer)
        {
            Vector pD = p2 - p1;

            if (Math.Abs(pD.X) >= Math.Abs(pD.Y))
            {
                if (p1.X < p2.X)
                {

                    for (int x = (int)p1.X; x <= (int)p2.X; x++)
                    {
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        this._drawSolidCircle(targetP, brushSize, color, layer);
                    }
                }
                else
                {
                    for (int x = (int)p2.X; x <= (int)p1.X; x++)
                    {
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        this._drawSolidCircle(targetP, brushSize, color, layer);
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
                        this._drawSolidCircle(targetP, brushSize, color, layer);
                    }
                }
                else
                {
                    for (int y = (int)p2.Y; y <= (int)p1.Y; y++)
                    {
                        Point targetP = new Point(Linear(y, tp1, tp2), (double)y);
                        this._drawSolidCircle(targetP, brushSize, color, layer);
                    }
                }
            }
        }

    }
}