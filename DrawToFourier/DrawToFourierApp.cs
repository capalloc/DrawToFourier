using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DrawToFourier
{

    internal class DrawToFourierApp : Application
    {
        private Window _drawWindow;

        [STAThread]
        public static void Main(string[] args)
        {
            System.Diagnostics.Debug.WriteLine("Starting app...");
            InitApp();
        }

        public static void InitApp()
        {
            DrawToFourierApp app = new DrawToFourierApp();
            app.Run();
        }

        public DrawToFourierApp() : base()
        {
            this.Startup += AppStartupHandler;
        }

        private void AppStartupHandler(object sender, StartupEventArgs e)
        {
            this.MainWindow = this._drawWindow = new DrawWindow();
            this.MainWindow.Show();
        }

    }
}
