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
using static DrawToFourier.Fourier.FourierCore;

namespace DrawToFourier.UI
{
    internal class ImageHandler : ImageSourceWrapper
    {
        // Draws a white 3x3 square (called a dot) on the given bitmap and point. If no pixel is updated on the bitmap, returns false, otherwise returns true.
        public static bool DrawDot(WriteableBitmap bmp, Point dotCenter)
        {
            Int32Rect rect = new Int32Rect(0, 0, 3, 3);
            int rectOriginX = 0;
            int rectOriginY = 0;

            if ((int)dotCenter.X > 0)
            {
                rectOriginX = (int)dotCenter.X - 1;

                if ((int)dotCenter.X > (int)bmp.Width)
                    return false;

                if ((int)dotCenter.X >= (int)bmp.Width - 1)
                    rect.Width = 1 + (int)bmp.Width - (int)dotCenter.X;
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

                if ((int)dotCenter.Y > (int)bmp.Height)
                    return false;

                if ((int)dotCenter.Y >= (int)bmp.Height - 1)
                    rect.Height = 1 + (int)bmp.Height - (int)dotCenter.Y;
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

            bmp.WritePixels(rect, colorArray, rect.Width * 4, rectOriginX, rectOriginY);

            return true;
        }

        // Draws a line with 3 pixel stroke on the given bitmap between given points.
        // It does this by linearly interpolating points between given input points and draws a dot on each of them.
        // Returns the last drawn point (right now return value may not be correct)
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
                        Point targetP = new Point((double)x, Linear(x, p1, p2));
                        if (!DrawDot(bmp, targetP))
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
                        if (!DrawDot(bmp, targetP))
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
                        if (!DrawDot(bmp, targetP))
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
                        if (!DrawDot(bmp, targetP))
                        {
                            //return lastTarget;
                        }
                        lastTarget = targetP;
                    }
                }
            }

            return lastTarget;
        }

        // Linear interpolation between two points
        public static double Linear(double x, Point p1, Point p2)
        {
            if ((p2.X - p1.X) == 0)
            {
                return (p1.Y + p2.Y) / 2;
            }
            return p1.Y + (x - p1.X) * (p2.Y - p1.Y) / (p2.X - p1.X);
        }

        // Returns a Cubic Bezier Function for easily calculating the bezier points between given two lines, with non-base points being the points of lines where bezier curve is
        // starting and ending (p0 and p3), and base points being the far end of those two lines (used for calculating slopes/sin/cos). 'lengthToDistanceFactor' determines how far
        // the reference points of p1 and p2 are from p0 and p3, as a factor of the distance between starting and ending points p0 and p3.
        public static Func<double, Point> cubicBezierGenerator(Point startPointBase, Point startPoint, Point endPoint, Point endPointBase, double lengthToDistanceFactor)
        {
            double distance = (endPoint - startPoint).Length;

            Vector normalizedStartVector = startPoint - startPointBase;
            Vector normalizedEndVector = endPoint - endPointBase;
            normalizedStartVector.Normalize();
            normalizedEndVector.Normalize();

            Vector[] pointVector = new Vector[] { 
                ((Vector)startPoint), 
                ((Vector)startPoint) + normalizedStartVector * distance * lengthToDistanceFactor, 
                ((Vector)endPoint) + normalizedEndVector * distance * lengthToDistanceFactor, 
                ((Vector)endPoint) 
            };

            return (double t) => {
                double[] multiplyVector = new double[] {
                    Math.Pow(1 - t, 3),
                    3 * Math.Pow(1 - t, 2) * t,
                    3 * (1 - t) * Math.Pow(t, 2),
                    Math.Pow(t, 3)
                };

                return (Point)pointVector.Zip(multiplyVector, (pV, m) => pV * m).Aggregate(new Vector(0, 0), (prev, next) => prev + next);
            };
        }

        private FourierCore fc;
        private WriteableBitmap _bmp;
        private Queue<Point> _captureSequence; // Holds 4 of the last drawn points. Empty when path is complete or not yet drawn.
        private bool _newlyEnteredCanvas; // Set true when the cursor first enters the image space after leaving. Set to false afterwards.

        // Used for retaining starting point data of the path for drawing bezier curve for the completion of the path
        private Point? _startPoint;
        private Point? _startPointBase;

        public ImageHandler(FourierCore fc)
        {
            // Initial image size should be a square with length equal to the half of smaller side of the user screen
            int length = Math.Min((int)(SystemParameters.PrimaryScreenWidth * 0.5), (int)(SystemParameters.PrimaryScreenHeight * 0.5));
            this.Source = this._bmp = new WriteableBitmap(length, length, 96, 96, PixelFormats.Bgr32, null);
            this._captureSequence = new Queue<Point>(4);
            this.fc = fc;
        }

        public override void OnMouseDown(double X, double Y, MouseButton clicked)
        {
            Point p = new Point(X, Y);

                switch (clicked)
                {
                    case MouseButton.Left:
                        if (this._captureSequence.Any()) // Should result in solid last path
                        {
                            if (this._captureSequence.Count < 4)
                            {
                                throw new NotImplementedException(); // If there are not enough path points to generate a bezier curve
                            }

                            Point l = this._captureSequence.Last();

                            if (!p.Equals(l)) // If not duplicate
                                DrawLine(this._bmp, p, l);

                            // Draw bezier curve between starting and ending of the path
                            Point[] bezierPoints = this._captureSequence.ToArray();

                            #pragma warning disable CS8629
                            Func<double, Point> bezierCalc = cubicBezierGenerator(bezierPoints[2], bezierPoints[3], (Point)this._startPoint, (Point)this._startPointBase, 0.5);

                            for (double t = 0; t < 1; t += 0.05)
                            {
                                Point bPoint = bezierCalc(t);
                                DrawLine(this._bmp, l, bPoint);
                                l = bPoint;
                            }
                            
                            DrawLine(this._bmp, l, bezierCalc(1));

                            this._captureSequence.Clear();
                            this._startPoint = null;
                            this._startPointBase = null;
                        }
                        else // Starting the path
                        {
                            this._captureSequence.Enqueue(p);
                            this._startPoint = p;
                            this._startPointBase = null;
                            DrawDot(this._bmp, p);
                        }
                        break;
                    case MouseButton.Right: // Should result in transparent last path
                        if (this._captureSequence.Any())
                        {
                            if (this._captureSequence.Count < 4)
                            {
                                throw new NotImplementedException(); // If there are not enough path points to generate a bezier curve
                            }

                            Point l = this._captureSequence.Last();

                            if (!p.Equals(l)) // If not duplicate
                                DrawLine(this._bmp, p, l);

                            // Draw bezier curve between starting and ending of the path
                            Point[] bezierPoints = this._captureSequence.ToArray();

                            #pragma warning disable CS8629
                            Func<double, Point> bezierCalc = cubicBezierGenerator(bezierPoints[2], bezierPoints[3], (Point)this._startPoint, (Point)this._startPointBase, 0.5);

                            for (double t = 0; t < 1; t += 0.05)
                            {
                                Point bPoint = bezierCalc(t);
                                DrawLine(this._bmp, l, bPoint);
                                l = bPoint;
                            }

                            DrawLine(this._bmp, l, bezierCalc(1));

                            this._captureSequence.Clear();
                            this._startPoint = null;
                            this._startPointBase = null;
                        }
                        break;
                }
        }

        public override void OnMouseLeave(double X, double Y)
        {
            if (this._captureSequence.Any())
            {
                Point p = new Point(X, Y);
                Point l = this._captureSequence.Last();

                if (p.Equals(l)) // If duplicate
                    return;

                DrawLine(this._bmp, p, l);
                this._captureSequence.Enqueue(p);

                if (this._captureSequence.Count > 4)
                    this._captureSequence.Dequeue();

                if (this._startPoint.Equals(l))
                    this._startPointBase = p;
            }
        }

        public override void OnMouseEnter(double X, double Y)
        {
            if (this._captureSequence.Any())
            {
                Point p = new Point(X, Y);
                Point l = this._captureSequence.Last();

                if (p.Equals(l)) // If duplicate
                    return;

                this._captureSequence.Enqueue(p);

                if (this._captureSequence.Count > 4)
                    this._captureSequence.Dequeue();

                this._newlyEnteredCanvas = true;

                if (this._startPoint.Equals(l))
                    this._startPointBase = p;
            }
        }

        public override void OnMouseMove(double X, double Y)
        {
            if (this._captureSequence.Any()) 
            {
                Point p = new Point(X, Y);
                Point l = this._captureSequence.Last();

                if (p.Equals(l)) // If duplicate
                    return;

                DrawLine(this._bmp, p, l);
                this._captureSequence.Enqueue(p);

                if (this._captureSequence.Count > 4)
                    this._captureSequence.Dequeue();

                if (this._startPoint.Equals(l))
                    this._startPointBase = p;

                // If this is the second point after entering the draw area, draw bezier curve between the point just before leaving
                // the draw area and point just after entering the draw area.
                if (this._newlyEnteredCanvas) 
                {
                    Point[] bezierPoints = this._captureSequence.ToArray();
                    Func<double, Point> bezierCalc = cubicBezierGenerator(bezierPoints[3], bezierPoints[2], bezierPoints[1], bezierPoints[0], 0.5);

                    for (double t = 0; t < 1; t += 0.05)
                    {
                        Point bPoint = bezierCalc(t);
                        DrawLine(this._bmp, l, bPoint);
                        l = bPoint;
                    }

                    DrawLine(this._bmp, l, bezierCalc(1));

                    this._newlyEnteredCanvas = false;
                }
            }
        }
    }
}
