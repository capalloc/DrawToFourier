using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DrawToFourier.Fourier;
using DrawToFourier.UI;

namespace DrawToFourier
{

    internal class MainApp : Application
    {
        
        public static void InitApp()
        {
            MainApp app = new MainApp();
            app.Run();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            InitApp();
        }

        private Window _drawWindow;
        private ImageHandler _imgHandlerDraw;
        private FourierCore _fourierCore;

        #pragma warning disable CS8618
        public MainApp() : base()
        {
            this._fourierCore = new FourierCore();
            this._imgHandlerDraw = new ImageHandler(this._fourierCore.OnPathAction);
            this.Startup += AppStartupHandler;
        }

        private void AppStartupHandler(object sender, StartupEventArgs e)
        {
            this.MainWindow = this._drawWindow = new DrawWindow(this._imgHandlerDraw, this._fourierCore.OnUIAction);
            this.MainWindow.Show();
        }

    }
}
