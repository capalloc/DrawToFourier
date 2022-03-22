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

            if (rX + w > this._pixelWidth)
            {
                w = this._pixelWidth - rX;
            }
            if (rY + h > this._pixelHeight)
            {
                h = this._pixelHeight - rY;
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
                        this._paintLayer(layer, (i + rY) * this._pixelWidth + rX + j, color);
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
                        this._paintLayer(layer, (i + rY) * this._pixelWidth + rX + j, color);
                }
            }
        }

        private void _drawLine(Point p1, Point p2, int brushSize, uint color, int layer)
        {
            double distance = (p2 - p1).Length;
            double cos = (p2.X - p1.X) / distance;
            double sin = (p2.Y - p1.Y) / distance;

            int gx1 = (int)p1.X;
            int gx2 = (int)p2.X;
            int gy1 = (int)p1.Y;
            int gy2 = (int)p2.Y;

            int ox1 = (int)(-brushSize * sin / 2);
            int ox2 = (int)(brushSize * sin / 2);
            int oy1 = (int)(brushSize * cos / 2);
            int oy2 = (int)(-brushSize * cos / 2);

            int osx = ox1 < ox2 ? 1 : -1;
            int osy = oy1 < oy2 ? 1 : -1;
            int odx = Math.Abs(ox2 - ox1);
            int ody = -Math.Abs(oy2 - oy1);
            int oerror = odx + ody;

            int ox1_old = ox1;
            int oy1_old = oy1;
            int dGx = 0;
            int dGy = 0;

            int x1, x2, y1, y2, x1_old, y1_old, dx, dy, sx, sy, error, dLx, dLy, tempx, tempy;

            while (true)
            {
                x1 = ox1 + gx1;
                x2 = ox1 + gx2;
                y1 = oy1 + gy1;
                y2 = oy1 + gy2;

                dx = Math.Abs(x2 - x1);
                dy = -Math.Abs(y2 - y1);
                sx = x1 < x2 ? 1 : -1;
                sy = y1 < y2 ? 1 : -1;
                error = dx + dy;

                if (Math.Abs(dGx) + Math.Abs(dGy) == 2)
                {
                    x1_old = x1;
                    y1_old = y1;
                    dLx = 0;
                    dLy = 0;

                    while (true)
                    {
                        if (0 <= y1 && y1 < this._pixelHeight && 0 <= x1 && x1 < this._pixelWidth)
                            this._paintLayer(layer, this._pixelWidth * y1 + x1, color);

                        tempx = (dLx - dGx) / 2 + x1_old;
                        tempy = (dLy - dGy) / 2 + y1_old;

                        if (Math.Abs(dLx) + Math.Abs(dLy) == 2 && 0 <= tempy && tempy < this._pixelHeight && 0 <= tempx && tempx < this._pixelWidth)
                            this._paintLayer(layer, this._pixelWidth * tempy + tempx, color);

                        x1_old = x1;
                        y1_old = y1;

                        if (x1 == x2 && y1 == y2)
                            break;

                        int e2 = 2 * error;

                        if (e2 >= dy)
                        {
                            if (x1 == x2) break;
                            error += dy;
                            x1 += sx;
                        }

                        if (e2 <= dx)
                        {
                            if (y1 == y2) break;
                            error += dx;
                            y1 += sy;
                        }

                        dLx = x1 - x1_old;
                        dLy = y1 - y1_old;
                    }
                } 
                else
                {
                    while (true)
                    {
                        if (0 <= y1 && y1 < this._pixelHeight && 0 <= x1 && x1 < this._pixelWidth)
                            this._paintLayer(layer, this._pixelWidth * y1 + x1, color);

                        if (x1 == x2 && y1 == y2)
                            break;

                        int e2 = 2 * error;

                        if (e2 >= dy)
                        {
                            if (x1 == x2) break;
                            error += dy;
                            x1 += sx;
                        }

                        if (e2 <= dx)
                        {
                            if (y1 == y2) break;
                            error += dx;
                            y1 += sy;
                        }
                    }
                }

                //

                if (ox1 == ox2 && oy1 == oy2) break;

                int oe2 = 2 * oerror;

                if (oe2 >= ody)
                {
                    if (ox1 == ox2) break;
                    oerror += ody;
                    ox1 += osx;
                }

                if (oe2 <= odx)
                {
                    if (oy1 == oy2) break;
                    oerror += odx;
                    oy1 += osy;
                }

                dGx = ox1 - ox1_old;
                dGy = oy1 - oy1_old;

                ox1_old = ox1;
                oy1_old = oy1;
            }

            /*
            // Calculate and draw across

            x1 = ox1 + gx1;
            x2 = ox1 + gx2;
            y1 = oy1 + gy1;
            y2 = oy1 + gy2;

            xChange = ox1 - ox1prev;
            yChange = oy1 - oy1prev;

            calc_and_draw:
            dx = Math.Abs(x2 - x1);
            dy = -Math.Abs(y2 - y1);
            sx = x1 < x2 ? 1 : -1;
            sy = y1 < y2 ? 1 : -1;
            error = dx + dy;

            while (true)
            {
                this._paintLayer(layer, this._pixelWidth * y1 + x1, color);

                if (x1 == x2 && y1 == y2)
                {
                    if (xChange == 0 || yChange == 0) break;

                    x1 = ox1 + gx1;
                    x2 = ox1 + gx2;
                    y1 = oy1 + gy1 - yChange;
                    y2 = oy1 + gy2 - yChange;

                    xChange = 0;
                    yChange = 0;
                    goto calc_and_draw;
                }

                int e2 = 2 * error;

                if (e2 >= dy)
                {
                    if (x1 == x2)
                    {
                        if (xChange == 0 || yChange == 0) break;

                        x1 = ox1 + gx1;
                        x2 = ox1 + gx2;
                        y1 = oy1 + gy1 - yChange;
                        y2 = oy1 + gy2 - yChange;

                        xChange = 0;
                        yChange = 0;
                        goto calc_and_draw;
                    }
                    error += dy;
                    x1 += sx;
                }

                if (e2 <= dx)
                {
                    if (y1 == y2)
                    {
                        if (xChange == 0 || yChange == 0) break;

                        x1 = ox1 + gx1;
                        x2 = ox1 + gx2;
                        y1 = oy1 + gy1 - yChange;
                        y2 = oy1 + gy2 - yChange;

                        xChange = 0;
                        yChange = 0;
                        goto calc_and_draw;
                    }
                    error += dx;
                    y1 += sy;
                }
            }

            // Pick next perpendicular

            ox1prev = ox1;
            oy1prev = oy1;


            if (ox1 == ox2 && oy1 == oy2) break;

            int oe2 = 2 * oerror;

            if (oe2 >= ody)
            {
                if (ox1 == ox2) break;
                oerror += ody;
                ox1 += osx;
            }

            if (oe2 <= odx)
            {
                if (oy1 == oy2) break;
                oerror += odx;
                oy1 += osy;
            }
        }*/

            /*Point leftP = Math.Min(p1.X, p2.X) == p1.X ? p1 : p2;
            Point rightP = leftP == p1 ? p2 : p1;

            double distance = (rightP - leftP).Length;
            double cos = (rightP.X - leftP.X) / distance;
            double sin = (rightP.Y - leftP.Y) / distance;

            this._drawSolidCircle(leftP, brushSize, color, layer);
            this._drawSolidCircle(rightP, brushSize, color, layer);

            int lowerY;
            int upperY;
            int lowerX;
            int upperX;

            if (sin >= 0)
            {
                lowerY = Math.Max((int)(leftP.Y - brushSize * cos / 2), 0);
                upperY = Math.Min((int)(rightP.Y + brushSize * cos / 2), this._pixelHeight - 1);

                for (int y = lowerY; y <= upperY; y++)
                {
                    if (y <= leftP.Y + brushSize * cos / 2 && cos != 0)
                    {
                        lowerX = Math.Max((int)(-sin * y / cos + sin * leftP.Y / cos + leftP.X), 0);
                    }
                    else
                    {
                        lowerX = Math.Max((int)(cos * y / sin - cos * leftP.Y / sin + leftP.X - brushSize / (2 * sin)), 0);
                    }

                    if (y <= rightP.Y - brushSize * cos / 2 && sin != 0)
                    {
                        upperX = Math.Min((int)(cos * y / sin - cos * leftP.Y / sin + leftP.X + brushSize / (2 * sin)), this._pixelWidth - 1);
                    }
                    else
                    {
                        upperX = Math.Min((int)(-sin * y / cos + sin * rightP.Y / cos + rightP.X), this._pixelWidth - 1);
                    }

                    for (int x = lowerX; x <= upperX; x++)
                    {
                        this._paintLayer(layer, this._pixelWidth * y + x, color);
                    }
                }
            } 
            else
            {
                lowerY = Math.Max((int)(rightP.Y - brushSize * cos / 2), 0);
                upperY = Math.Min((int)(leftP.Y + brushSize * cos / 2), this._pixelHeight - 1);

                for (int y = lowerY; y <= upperY; y++)
                {
                    if (y <= leftP.Y - brushSize * cos / 2 && sin != 0)
                    {
                        lowerX = Math.Max((int)(cos * y / sin - cos * leftP.Y / sin + leftP.X + brushSize / (2 * sin)), 0);
                    }
                    else
                    {
                        lowerX = Math.Max((int)(-sin * y / cos + sin * leftP.Y / cos + leftP.X), 0);
                    }

                    if (y <= rightP.Y + brushSize * cos / 2 && cos != 0)
                    {
                        upperX = Math.Min((int)(-sin * y / cos + sin * rightP.Y / cos + rightP.X), this._pixelWidth - 1);
                    }
                    else
                    {
                        upperX = Math.Min((int)(cos * y / sin - cos * leftP.Y / sin + leftP.X - brushSize / (2 * sin)), this._pixelWidth - 1);
                    }

                    for (int x = lowerX; x <= upperX; x++)
                    {
                        this._paintLayer(layer, this._pixelWidth * y + x, color);
                    }
                }
            }*/

            /*Vector pD = p2 - p1;

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
            }*/
        }

    }
}