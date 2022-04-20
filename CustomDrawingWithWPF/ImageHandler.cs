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
        public static double Linear(double x, Point p0, Point p1)
        {
            if ((p1.X - p0.X) == 0)
            {
                return (p0.Y + p1.Y) / 2;
            }
            return p0.Y + (x - p0.X) * (p1.Y - p0.Y) / (p1.X - p0.X);
        }

        private static uint CompositeColor(uint backgroundColor, uint addedColor)
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

        private static uint BrushColorToUint(byte r, byte g, byte b, byte a)
        {
            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }

        private WriteableBitmap _bmp;
        private uint[] _buffer;
        private int _pixelWidth;
        private int _pixelHeight;
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
            this._pixelWidth = this._bmp.PixelWidth;
            this._pixelHeight = this._bmp.PixelHeight;
            this._pixelCount = this._bmp.PixelWidth * this._bmp.PixelHeight;

            this._layers = new uint[maxLayerCount + 1][];
            this._layersComposed = new uint[maxLayerCount + 1][];

            this._layerChangedPixels = new int[maxLayerCount + 1][];
            this._layerAllChangesPixels = new int[maxLayerCount + 1][];

            this._layerIsPixelChanged = new bool[maxLayerCount + 1][];
            this._layerIsPixelAllChanges = new bool[maxLayerCount + 1][];

            this._layers[0] = new uint[this._pixelCount];
            this._layersComposed[0] = this._layers[0];
            Array.Fill(this._layers[0], BrushColorToUint(bR, bG, bB, bA));

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
            this._bmp.WritePixels(new Int32Rect(0, 0, this._pixelWidth, this._pixelHeight), this._buffer, 4 * this._pixelWidth, 0);
        }

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
                            uint newColor = CompositeColor(this._layersComposed[(int)prevLayer!][pixelLoc], this._layers[currLayer][pixelLoc]);

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
                            uint newColor = CompositeColor(this._layersComposed[(int)prevLayer!][pixelLoc], this._layers[currLayer][pixelLoc]);

                            if (this._layersComposed[currLayer][pixelLoc] == newColor) continue;
                            this._layersComposed[currLayer][pixelLoc] = newColor;
                        }
                    }

                    this._layerIsPixelChanged[currLayer] = new bool[this._pixelCount];
                }

                currLayer = nextLayer == null ? -1 : (int)nextLayer;
            } while (nextLayer != null);
        }

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

        // Draws a hollow circle at given point with given diameter
        public void DrawHollowCircle(Point circleCenter, int diameter, int brushSize, byte r, byte g, byte b, byte a = 255, int layer = 1)
        {
            this._initializeLayerIfNotExist(layer);
            this._drawHollowCircle(circleCenter, diameter, brushSize, BrushColorToUint(r, g, b, a), layer);
        }

        // Draws a filled circle at given point with given diameter
        public void DrawSolidCircle(Point circleCenter, int diameter, byte r, byte g, byte b, byte a = 255, int layer = 1)
        {
            this._initializeLayerIfNotExist(layer);
            this._drawSolidCircle(circleCenter, diameter, BrushColorToUint(r, g, b, a), layer);
        }

        // Draws a line with 3 pixel stroke on current bitmap between given points.
        // It does this by linearly interpolating points between given input points and draws a dot on each of them.
        // Returns the last drawn point (right now return value may not be correct)
        public void DrawLine(Point p0, Point p1, int brushSize, byte r, byte g, byte b, byte a = 255, int layer = 1)
        {
            this._initializeLayerIfNotExist(layer);
            this._drawLine(p0, p1, brushSize, BrushColorToUint(r, g, b, a), layer);
        }

        private void _initializeLayerIfNotExist(int layer)
        {
            if (layer < 1) throw new ArgumentOutOfRangeException(nameof(layer));

            if (this._layers[layer] == null)
            {
                this._layers[layer] = new uint[this._pixelCount];
                this._layersComposed[layer] = new uint[this._pixelCount];
                this._layerChangedPixels[layer] = new int[this._pixelCount];
                this._layerIsPixelChanged[layer] = new bool[this._pixelCount];
                this._layerAllChangesPixels[layer] = new int[this._pixelCount];
                this._layerIsPixelAllChanges[layer] = new bool[this._pixelCount];

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

        private void _paintLayer(int layer, int x, int y, uint color)
        {
            int pixel = this._pixelWidth * y + x;

            if (x >= 0 && y >= 0 && this._pixelWidth > x && this._pixelHeight > y && this._layers[layer][pixel] != color)
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
            if (diameter == 0) return;

            double radius = diameter / 2;
            double unitAngle = brushSize / radius;

            Point prevPoint = new Point(circleCenter.X + radius, circleCenter.Y);

            double t;

            for (t = unitAngle; t < 2 * Math.PI; t += unitAngle)
            {
                Point p = new Point(circleCenter.X + radius * Math.Cos(t), circleCenter.Y + radius * Math.Sin(t));
                this._drawLine(p, prevPoint, brushSize, color, layer);
                prevPoint = p;
            }

            this._drawLine(new Point(circleCenter.X + radius * Math.Cos(t + unitAngle), circleCenter.Y + radius * Math.Sin(t + unitAngle)), prevPoint, brushSize, color, layer);
        }

        private void _drawSolidCircle(Point circleCenter, int diameter, uint color, int layer)
        {
            if (diameter == 0)
                return;

            int cx = (int)circleCenter.X;
            int cy = (int)circleCenter.Y;
            int radius = diameter / 2;

            if (radius == 0)
            {
                this._paintLayer(layer, cx, cy, color);
                return;
            }

            int x = 0;
            int y = radius;
            int d = 3 - 2 * radius;

            this._paintLayer(layer, x + cx, y + cy, color);
            this._paintLayer(layer, x + cx, -y + cy, color);

            for (int i = -y; i <= y; i++)
                this._paintLayer(layer, i + cx, x + cy, color);

            while (x < y)
            {
                if (d < 0) 
                {
                    d += (4 * ++x) + 6;

                    for (int i = -y; i <= y; i++)
                    {
                        this._paintLayer(layer, i + cx, x + cy, color);
                        this._paintLayer(layer, i + cx, -x + cy, color);
                    }

                    this._paintLayer(layer,  x + cx,  y + cy,  color);
                    this._paintLayer(layer, -x + cx,  y + cy,  color);
                    this._paintLayer(layer, -x + cx, -y + cy,  color);
                    this._paintLayer(layer,  x + cx, -y + cy,  color);
                }
                else
                {
                    d += 4 * (++x - y--) + 10;

                    for (int i = -y; i <= y; i++)
                    {
                        this._paintLayer(layer, i + cx, x + cy, color);
                        this._paintLayer(layer, i + cx, -x + cy, color);
                    }

                    for (int i = -x; i <= x; i++)
                    {
                        this._paintLayer(layer, i + cx, y + cy, color);
                        this._paintLayer(layer, i + cx, -y + cy, color);
                    }
                }
            }
        }

        private void _drawLine(Point p0, Point p1, int brushSize, uint color, int layer)
        {
            int x0 = (int)p0.X;
            int y0 = (int)p0.Y;
            int x1 = (int)p1.X;
            int y1 = (int)p1.Y;

            int dx = x1 - x0;
            int dy = y1 - y0;
            int xStep = 1;
            int yStep = 1;

            if (dx < 0) 
            { 
                dx = -dx;
                xStep = -1; 
            }
            else if (dx == 0) 
            {
                xStep = 0; 
            }

            if (dy < 0) 
            { 
                dy = -dy; 
                yStep = -1;
            }
            else if (dy == 0) 
            { 
                yStep = 0; 
            }

            int pxStep = 0;
            int pyStep = 0;

            switch (xStep + yStep * 4)
            {
                case -5: pyStep = -1; pxStep = 1;
                    break;   
                case -1: pyStep = -1; 
                    break;   
                case  3: pyStep = 1; pxStep = 1; 
                    break;  
                case -4: pxStep = -1;
                    break;
                case  4: pxStep = 1; 
                    break;  
                case -3: pyStep = -1; pxStep = -1;
                    break;  
                case  1: pyStep = -1;
                    break; 
                case  5: pyStep = 1; pxStep = -1;
                    break;
                case  0: return;
            }

            int x = x0;
            int y = y0;

            int err = 0;
            int errP = 0;

            int w = (int)(brushSize * Math.Sqrt(dx * dx + dy * dy));

            if (dx > dy) // X Based Lines
            {
                int errDg = -2 * dx;
                int errSq = 2 * dy;
                int threshold = dx - 2 * dy;

                for (int p = 0; p <= dx; p++)
                {
                    // X Based Perpendicular Lines

                    int x_perp = x;
                    int y_perp = y;
                    int p_perp = 0;
                    int q_perp = 0;

                    int err_perp = errP;
                    int tk = dx + dy - err;

                    while (tk <= w)
                    {
                        this._paintLayer(layer, x_perp, y_perp, color);

                        if (err_perp >= threshold)
                        {
                            x_perp += pxStep;
                            err_perp += errDg;
                            tk += 2 * dy;
                        }

                        err_perp += errSq;
                        y_perp += pyStep;
                        tk += 2 * dx;
                        q_perp++;
                    }

                    x_perp = x;
                    y_perp = y;

                    err_perp = -errP;
                    tk = dx + dy + err;

                    while (tk <= w)
                    {
                        if (p_perp > 0)
                            this._paintLayer(layer, x_perp, y_perp, color);

                        if (err_perp > threshold)
                        {
                            x_perp -= pxStep;
                            err_perp += errDg;
                            tk += 2 * dy;
                        }

                        err_perp += errSq;
                        y_perp -= pyStep;
                        tk += 2 * dx;
                        p_perp++;
                    }

                    if (q_perp == 0 && p_perp < 2) this._paintLayer(layer, x, y, color);

                    // End

                    if (err >= threshold)
                    {
                        y += yStep;
                        err += errDg;

                        if (errP >= threshold)
                        {
                            // X Based Perpendicular Lines (Square Move)

                            x_perp = x;
                            y_perp = y;
                            p_perp = 0;
                            q_perp = 0;

                            err_perp = errP + errDg + errSq;
                            tk = dx + dy - err;

                            while (tk <= w)
                            {
                                this._paintLayer(layer, x_perp, y_perp, color);

                                if (err_perp >= threshold)
                                {
                                    x_perp += pxStep;
                                    err_perp += errDg;
                                    tk += 2 * dy;
                                }

                                err_perp += errSq;
                                y_perp += pyStep;
                                tk += 2 * dx;
                                q_perp++;
                            }

                            x_perp = x;
                            y_perp = y;

                            err_perp = -(errP + errDg + errSq);
                            tk = dx + dy + err;

                            while (tk <= w)
                            {
                                if (p_perp > 0)
                                    this._paintLayer(layer, x_perp, y_perp, color);

                                if (err_perp > threshold)
                                {
                                    x_perp -= pxStep;
                                    err_perp += errDg;
                                    tk += 2 * dy;
                                }

                                err_perp += errSq;
                                y_perp -= pyStep;
                                tk += 2 * dx;
                                p_perp++;
                            }

                            if (q_perp == 0 && p_perp < 2) this._paintLayer(layer, x, y, color);

                            // End

                            errP += errDg;
                        }

                        errP += errSq;
                    }

                    err += errSq;
                    x += xStep;
                }
            }
            else // Y Based Lines
            {
                int errDg = -2 * dy;
                int errSq = 2 * dx;
                int threshold = dy - 2 * dx;

                for (int p = 0; p <= dy; p++)
                {
                    
                    // Y Based Perpendicular Lines

                    int x_perp = x;
                    int y_perp = y;
                    int p_perp = 0;
                    int q_perp = 0;

                    int err_perp = -errP;
                    int tk = dx + dy + err;

                    while (tk <= w)
                    {
                        this._paintLayer(layer, x_perp, y_perp, color);

                        if (err_perp > threshold)
                        {
                            y_perp += pyStep;
                            err_perp += errDg;
                            tk += 2 * dx;
                        }

                        err_perp += errSq;
                        x_perp += pxStep;
                        tk += 2 * dy;
                        q_perp++;
                    }

                    x_perp = x;
                    y_perp = y;

                    err_perp = errP;
                    tk = dx + dy - err;

                    while (tk <= w)
                    {
                        if (p_perp > 0)
                            this._paintLayer(layer, x_perp, y_perp, color);

                        if (err_perp >= threshold)
                        {
                            y_perp -= pyStep;
                            err_perp += errDg;
                            tk += 2 * dx;
                        }

                        err_perp += errSq;
                        x_perp -= pxStep;
                        tk += 2 * dy;
                        p_perp++;
                    }

                    if (q_perp == 0 && p_perp < 2) this._paintLayer(layer, x, y, color);

                    // End

                    if (err >= threshold)
                    {
                        x += xStep;
                        err += errDg;

                        if (errP >= threshold)
                        {

                            // Y Based Perpendicular Lines (Square Move)

                            x_perp = x;
                            y_perp = y;
                            p_perp = 0;
                            q_perp = 0;

                            err_perp = -(errP + errDg + errSq);
                            tk = dx + dy + err;

                            while (tk <= w)
                            {
                                this._paintLayer(layer, x_perp, y_perp, color);

                                if (err_perp > threshold)
                                {
                                    y_perp += pyStep;
                                    err_perp += errDg;
                                    tk += 2 * dx;
                                }

                                err_perp += errSq;
                                x_perp += pxStep;
                                tk += 2 * dy;
                                q_perp++;
                            }

                            x_perp = x;
                            y_perp = y;

                            err_perp = errP + errDg + errSq;
                            tk = dx + dy - err;

                            while (tk <= w)
                            {
                                if (p_perp > 0)
                                    this._paintLayer(layer, x_perp, y_perp, color);

                                if (err_perp >= threshold)
                                {
                                    y_perp -= pyStep;
                                    err_perp += errDg;
                                    tk += 2 * dx;
                                }

                                err_perp += errSq;
                                x_perp -= pxStep;
                                tk += 2 * dy;
                                p_perp++;
                            }

                            if (q_perp == 0 && p_perp < 2) this._paintLayer(layer, x, y, color);

                            // End

                            errP += errDg;
                        }

                        errP += errSq;
                    }

                    err += errSq;
                    y += yStep;
                }
            }
        }
    }
}