using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CustomDrawingWithWPF;
using DrawToFourier.Fourier;
using DrawToFourier.UI;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DrawToFourier
{
    internal class MainApp : Application
    {
        private static readonly int defaultBrushSize = 6;
        private static readonly int defaultFourierCircleCount = 20;
        private static readonly double imageResolutionToActualRatio = 5.0;

        [STAThread]
        public static void Main(string[] args)
        {
            MainApp app = new MainApp();
            app.Run();
        }

        private int _initialDrawAreaLength;
        private int _initialImageLength;

        private DrawWindow _drawWindow;
        private ResultWindow _resultWindow;
        private ImageHandler _imgHandlerDraw;
        private ImageHandler _imgHandlerResult;
        private Point _lastMouseEventLocation;

        private LinkedList<FourierCore> _fouriers;
        private LinkedList<Path> _completedPaths;
        private Path? _activePath;

        public MainApp() : base()
        {
            // Initial draw area size should be a square with length equal to the half of smaller side of the user screen
            this._initialDrawAreaLength = Math.Min((int)(SystemParameters.PrimaryScreenWidth * 0.5), (int)(SystemParameters.PrimaryScreenHeight * 0.5));
            // Image size (resolution) should be 'imageResolutionToActualRatio' times the initial draw area
            this._initialImageLength = (int)Math.Round(this._initialDrawAreaLength * imageResolutionToActualRatio);

            this._imgHandlerDraw = new ImageHandler(this._initialImageLength, this._initialImageLength, 1);
            this._imgHandlerResult = new ImageHandler(this._initialImageLength, this._initialImageLength, 2);
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
                this._fouriers.AddLast(new FourierCore(path, defaultFourierCircleCount));

            this._resultWindow = new ResultWindow(this._imgHandlerResult, this._initialDrawAreaLength, this._initialDrawAreaLength);
            this._resultWindow.Show();
        }

        private void AppStartupHandler(object sender, StartupEventArgs e)
        {
            this.MainWindow = this._drawWindow = new DrawWindow(this._imgHandlerDraw, this._initialDrawAreaLength, this._initialDrawAreaLength);
            this.MainWindow.Show();
        }

        private void DrawLines(LinkedList<Line> lines)
        {
            foreach (Line line in lines)
                if (line.IsSolid) this._imgHandlerDraw.DrawLine(line.Start, line.End, defaultBrushSize, 255, 255, 255);

            this._imgHandlerDraw.ComposeLayers();
            this._imgHandlerDraw.RenderBuffer();
        }
    }
}
