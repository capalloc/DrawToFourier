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

        private LinkedList<Path> completedPaths;
        private Path activePath;

        #pragma warning disable CS8618
        public MainApp() : base()
        {
            this._fourierCore = new FourierCore();
            this._imgHandlerDraw = new ImageHandler();
            this.completedPaths = new LinkedList<Path>();
            this.activePath = new Path();
            this.Startup += AppStartupHandler;
        }

        public void OnMouseDown(double x, double y, MouseButton changedButton)
        {
            throw new NotImplementedException();
        }

        public void OnMouseLeave(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void OnMouseEnter(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void OnMouseMove(double x, double y)
        {
            throw new NotImplementedException();
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
