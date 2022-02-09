using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DrawToFourier.UI
{
    public struct Line
    {
        public Point start;
        public Point end;
        
        public override string ToString() {
            return $"({start},{end})";
        }
    }

    public partial class DrawWindow : Window
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

            rect.X = (int) dotCenter.X - 1;
            rect.Y = (int) dotCenter.Y - 1;

            if ((int) dotCenter.X > 0 && (int) dotCenter.X < bmp.Width - 1 && (int) dotCenter.Y > 0 && (int) dotCenter.Y < bmp.Height - 1)
            {
                bmp.CopyPixels(rect, colorArray, 12, 0);

                for(int i = 0; i < dotColorArray.Length; i++)
                {
                    if (dotColorArray[i] != 0xFFFFFFFF)
                        colorArray[i] = dotColorArray[i];
                }

                bmp.WritePixels(dotRect, colorArray, 12, (int)dotCenter.X - 1, (int)dotCenter.Y - 1);

                return true;
            } else {
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
                    for (int y = (int)p1.Y; y <= (int)p2.Y; y++) {
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

        private WriteableBitmap _drawBitmap;
        private bool _isDrawing;
        private Point _lastPos;
        private LinkedList<Line> _lines;

        public DrawWindow()
        {
            this._isDrawing = false;
            this._lastPos = new Point(-1, -1);
            this._lines = new LinkedList<Line>();

            InitializeComponent();
            this.Loaded += DrawWindowInitialLoadHandler;
        }

        private void DrawWindowInitialLoadHandler(object sender, RoutedEventArgs e)
        {

            this.Loaded -= DrawWindowInitialLoadHandler;
            this._drawBitmap = new WriteableBitmap((int)DrawCanvas.ActualWidth, (int)DrawCanvas.ActualHeight, 96, 96, PixelFormats.Bgr32, null);
            DrawImage.Source = this._drawBitmap;
        }

        private void DrawCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !this._isDrawing)
                this._isDrawing = true;
            else if (e.ChangedButton == MouseButton.Right && this._isDrawing) {
                this._isDrawing = false;
                this._lastPos = new Point(-1, -1);
                foreach (Line line in this._lines) {
                    System.Diagnostics.Debug.WriteLine($"{line}\n");
                }
                
            }
        }

        private void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this._isDrawing)
                return;

           
            if (this._lastPos.X == -1 && this._lastPos.Y == -1)
            {
                DrawDot(this._drawBitmap, e.GetPosition(DrawImage));
            } 
            else
            {
                Point lastSuccessfulPoint = DrawLine(this._drawBitmap, e.GetPosition(DrawImage), _lastPos);

                if (!lastSuccessfulPoint.Equals(e.GetPosition(DrawImage)))
                {
                    Line line = new();
                    line.start = lastSuccessfulPoint;
                    line.end = e.GetPosition(DrawImage);
                    this._lines.AddLast(line);
                }
            }

            this._lastPos = e.GetPosition(DrawImage);
        }
    }
}

