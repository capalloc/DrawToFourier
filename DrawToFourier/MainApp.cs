using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DrawToFourier.Fourier;
using DrawToFourier.UI;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DrawToFourier
{
    internal class MainApp : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            MainApp app = new MainApp();
            app.Run();
        }

        public Path ActivePath { get; }

        private Window _drawWindow;
        private ImageHandler _imgHandlerDraw;
        private FourierCore _fourierCore;
        private Point lastMouseEventLocation;

        private LinkedList<Path> completedPaths;
        private Path? activePath;

        public MainApp() : base()
        {
            this._fourierCore = new FourierCore();
            this._imgHandlerDraw = new ImageHandler();
            this.completedPaths = new LinkedList<Path>();
            this.Startup += AppStartupHandler;
        }

        public void OnMouseDown(double x, double y, MouseButton changedButton)
        {
            if (changedButton != MouseButton.Left && changedButton != MouseButton.Right)
                return;

            Point p = new Point(x, y);

            if (this.activePath == null)  // If path is not created yet
            {
                this.activePath = new Path(p);
                this._imgHandlerDraw.DrawDot(p);
            }
            else
            {
                if (!lastMouseEventLocation.Equals(p))  // If point is not duplicate
                {
                    LinkedList<Line> addedLines = this.activePath.addPoint(p);

                    foreach (Line line in addedLines)
                        this._imgHandlerDraw.DrawLine(line.Start, line.End);
                }

                if (this.activePath.LineCount < 1)   // If the path consists of a single point
                {
                    this.activePath = null;
                    return;
                }

                switch (changedButton)
                {
                    case MouseButton.Left:
                        LinkedList<Line> addedLines = this.activePath.finishSolid();

                        foreach (Line line in addedLines)
                            this._imgHandlerDraw.DrawLine(line.Start, line.End);

                        break;
                    case MouseButton.Right:
                        this.activePath.finishTransparent();
                        break;
                }

                this.completedPaths.AddLast(this.activePath);
                this.activePath = null;
            }

            lastMouseEventLocation = p;
        }

        public void OnMouseMove(double x, double y)
        {
            if (this.activePath == null)
                return;

            Point p = new Point(x, y);

            if (!lastMouseEventLocation.Equals(p))  // If point is not duplicate
            {
                LinkedList<Line> addedLines = this.activePath.addPoint(p);

                foreach (Line line in addedLines)
                    this._imgHandlerDraw.DrawLine(line.Start, line.End);

                lastMouseEventLocation = p;
            }
        }

        public void OnMouseLeave(double x, double y)
        {
            if (this.activePath == null)
                return;

            Point p = new Point(x, y);

            if (!lastMouseEventLocation.Equals(p))  // If point is not duplicate
            {
                LinkedList<Line> addedLines = this.activePath.addPoint(p);

                foreach (Line line in addedLines)
                    this._imgHandlerDraw.DrawLine(line.Start, line.End);

                lastMouseEventLocation = p;
            }

            this.activePath.BezierEnabled = true;
        }

        public void OnMouseEnter(double x, double y)
        {
            if (this.activePath == null)
                return;

            Point p = new Point(x, y);

            if (!lastMouseEventLocation.Equals(p))  // If point is not duplicate
            {
                LinkedList<Line> addedLines = this.activePath.addPoint(p);

                foreach (Line line in addedLines)
                    this._imgHandlerDraw.DrawLine(line.Start, line.End);

                lastMouseEventLocation = p;
            }
        }

        public void Load()
        {
            throw new NotImplementedException();
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Simulate()
        {
            throw new NotImplementedException();
        }

        private void AppStartupHandler(object sender, StartupEventArgs e)
        {
            this.MainWindow = this._drawWindow = new DrawWindow(this._imgHandlerDraw);
            this.MainWindow.Show();
        }

    }
}
