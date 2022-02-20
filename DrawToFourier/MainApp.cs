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

        private DrawWindow _drawWindow;
        private ImageHandler _imgHandlerDraw;
        private Point _lastMouseEventLocation;

        private LinkedList<FourierCore> _fouriers;
        private LinkedList<Path> _completedPaths;
        private Path? _activePath;

        public MainApp() : base()
        {
            this._imgHandlerDraw = new ImageHandler();
            this._completedPaths = new LinkedList<Path>();
            this._fouriers = new LinkedList<FourierCore>();
            this.Startup += AppStartupHandler;
        }

        // Mouse Events From 'DrawWindow'

        public void OnMouseDown(double x, double y, MouseButton changedButton)
        {
            if (changedButton != MouseButton.Left && changedButton != MouseButton.Right) // Only left and right mouse buttons should do something
                return;

            Point p = new Point(x, y);

            if (this._activePath == null)  // If path is not created yet
            {
                this._activePath = new Path(p);
                this._drawWindow.LoadButtonEnabled = false;
                this._drawWindow.SaveButtonEnabled = false;
                this._drawWindow.ResetButtonEnabled = false;
                this._drawWindow.SimulateButtonEnabled = false;
            }
            else
            {
                this._drawWindow.LoadButtonEnabled = false; // Temporarily disabled
                this._drawWindow.SaveButtonEnabled = false; // Temporarily disabled
                this._drawWindow.ResetButtonEnabled = true;
                this._drawWindow.SimulateButtonEnabled = true;

                LinkedList<Line> addedLines = new LinkedList<Line>();

                if (!_lastMouseEventLocation.Equals(p))  // If point is not duplicate
                {
                    addedLines = this._activePath.AddPoint(p);
                    DrawLines(addedLines);
                }

                if (this._activePath.LineCount < 1)   // If the path consists of a single point, delete path
                {
                    this._activePath = null;
                    return;
                }

                switch (changedButton)
                {
                    case MouseButton.Left:
                        addedLines = this._activePath.Finish(true);
                        break;
                    case MouseButton.Right:
                        addedLines = this._activePath.Finish(false);
                        break;
                }

                DrawLines(addedLines);
                this._completedPaths.AddLast(this._activePath);
                this._activePath = null;
            }

            _lastMouseEventLocation = p;
        }

        public void OnMouseMove(double x, double y)
        {
            if (this._activePath == null)
                return;

            Point p = new Point(x, y);

            if (!_lastMouseEventLocation.Equals(p))  // If point is not duplicate
            {
                LinkedList<Line> addedLines = this._activePath.AddPoint(p);
                DrawLines(addedLines);
                _lastMouseEventLocation = p;
            }
        }

        public void OnMouseLeave(double x, double y)
        {
            OnMouseMove(x, y);

            if (this._activePath != null)
                this._activePath.SetBezierNext(); // Leaving the draw area triggers bezier drawing for the next two 'addPoint' calls.
        }

        public void OnMouseEnter(double x, double y)
        {
            OnMouseMove(x, y); // Mouse entering the draw area doesn't do anything special. Out of boundary operations are handled in 'OnMouseMove' and 'Path'
        }

        // Button Events From 'DrawWindow'

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
            foreach (Path path in this._completedPaths)
                this._fouriers.AddLast(new FourierCore(path));
        }

        private void AppStartupHandler(object sender, StartupEventArgs e)
        {
            this.MainWindow = this._drawWindow = new DrawWindow(this._imgHandlerDraw);
            this.MainWindow.Show();
        }

        private void DrawLines(LinkedList<Line> lines)
        {
            foreach (Line line in lines)
                if (line.IsSolid) this._imgHandlerDraw.DrawLine(line.Start, line.End);
        }
    }
}
