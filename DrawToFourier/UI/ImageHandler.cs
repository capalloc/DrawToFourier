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

        public static double Linear(double x, Point p1, Point p2)
        {
            if ((p2.X - p1.X) == 0)
            {
                return (p1.Y + p2.Y) / 2;
            }
            return p1.Y + (x - p1.X) * (p2.Y - p1.Y) / (p2.X - p1.X);
        }

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

        public event CoreProgramActionEventHandler? ProgramAction;

        private WriteableBitmap _bmp;
        private Queue<Point> _captureSequence;
        private Point? startPoint;
        private Point? startPointBase;
        private bool _newlyEnteredCanvas;

        public ImageHandler(CoreProgramActionEventHandler programAction)
        {
            int length = Math.Min((int)(SystemParameters.PrimaryScreenWidth * 0.5), (int)(SystemParameters.PrimaryScreenHeight * 0.5));
            this.Source = this._bmp = new WriteableBitmap(length, length, 96, 96, PixelFormats.Bgr32, null);
            ProgramAction += programAction;
            this._captureSequence = new Queue<Point>(4);
        }

        public override void OnMouseDown(double X, double Y, MouseButton clicked)
        {
            if (this.ProgramAction != null)
            {
                Point p = new Point(X, Y);

                switch (clicked)
                {
                    case MouseButton.Left:
                        if (this._captureSequence.Any()) // Should result in solid last path
                        {
                            if (this._captureSequence.Count < 4)
                            {
                                throw new NotImplementedException();
                            }

                            Point l = this._captureSequence.Last();

                            if (!p.Equals(l))
                                DrawLine(this._bmp, p, l);

                            Point[] bezierPoints = this._captureSequence.ToArray();

                            #pragma warning disable CS8629
                            Func<double, Point> bezierCalc = cubicBezierGenerator(bezierPoints[2], bezierPoints[3], (Point)this.startPoint, (Point)this.startPointBase, 0.5);

                            for (double t = 0; t < 1; t += 0.05)
                            {
                                Point bPoint = bezierCalc(t);
                                DrawLine(this._bmp, l, bPoint);
                                l = bPoint;
                            }
                            
                            DrawLine(this._bmp, l, bezierCalc(1));

                            this._captureSequence.Clear();
                            this.startPoint = null;
                            this.startPointBase = null;
                        }
                        else
                        {
                            this._captureSequence.Enqueue(p);
                            this.startPoint = p;
                            this.startPointBase = null;
                            DrawDot(this._bmp, p);
                        }
                        break;
                    case MouseButton.Right: // Should result in transparent last path
                        if (this._captureSequence.Any())
                        {
                            if (this._captureSequence.Count < 4)
                            {
                                throw new NotImplementedException();
                            }

                            Point l = this._captureSequence.Last();

                            if (!p.Equals(l))
                                DrawLine(this._bmp, p, l);

                            Point[] bezierPoints = this._captureSequence.ToArray();

                            #pragma warning disable CS8629
                            Func<double, Point> bezierCalc = cubicBezierGenerator(bezierPoints[2], bezierPoints[3], (Point)this.startPoint, (Point)this.startPointBase, 0.5);

                            for (double t = 0; t < 1; t += 0.05)
                            {
                                Point bPoint = bezierCalc(t);
                                DrawLine(this._bmp, l, bPoint);
                                l = bPoint;
                            }

                            DrawLine(this._bmp, l, bezierCalc(1));

                            this._captureSequence.Clear();
                            this.startPoint = null;
                            this.startPointBase = null;
                        }
                        break;
                }
                
                this.ProgramAction.Invoke(this, new CoreProgramActionEventArgs("Down", X, Y));
            }
        }

        public override void OnMouseLeave(double X, double Y)
        {
            if (this.ProgramAction != null && this._captureSequence.Any())
            {
                Point p = new Point(X, Y);
                Point l = this._captureSequence.Last();

                if (p.Equals(l))
                    return;

                DrawLine(this._bmp, p, l);
                this._captureSequence.Enqueue(p);

                if (this._captureSequence.Count > 4)
                    this._captureSequence.Dequeue();

                if (this.startPoint.Equals(l))
                    this.startPointBase = p;

                this.ProgramAction.Invoke(this, new CoreProgramActionEventArgs("Leave", X, Y));
            }
        }

        public override void OnMouseEnter(double X, double Y)
        {
            if (this.ProgramAction != null && this._captureSequence.Any())
            {
                Point p = new Point(X, Y);
                Point l = this._captureSequence.Last();

                if (p.Equals(l))
                    return;

                this._captureSequence.Enqueue(p);

                if (this._captureSequence.Count > 4)
                    this._captureSequence.Dequeue();

                this._newlyEnteredCanvas = true;

                if (this.startPoint.Equals(l))
                    this.startPointBase = p;

                this.ProgramAction.Invoke(this, new CoreProgramActionEventArgs("Enter", X, Y));
            }
        }

        public override void OnMouseMove(double X, double Y)
        {
            if (this.ProgramAction != null && this._captureSequence.Any()) 
            {
                Point p = new Point(X, Y);
                Point l = this._captureSequence.Last();

                if (p.Equals(l))
                    return;

                DrawLine(this._bmp, p, l);
                this._captureSequence.Enqueue(p);

                if (this._captureSequence.Count > 4)
                    this._captureSequence.Dequeue();

                if (this.startPoint.Equals(l))
                    this.startPointBase = p;

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

                this.ProgramAction.Invoke(this, new CoreProgramActionEventArgs("Move", X, Y));
            }
        }
    }
}
