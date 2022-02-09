using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using DrawToFourier.UI;

namespace DrawToFourier
{

    internal class MainApp : Application
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
            MainApp app = new MainApp();
            app.Run();
        }

        public MainApp() : base()
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
