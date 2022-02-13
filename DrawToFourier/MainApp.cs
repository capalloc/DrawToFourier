using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            System.Diagnostics.Debug.WriteLine(System.Windows.SystemParameters.PrimaryScreenHeight);
            System.Diagnostics.Debug.WriteLine(System.Windows.SystemParameters.PrimaryScreenWidth);
            InitApp();
        }

        private Window _drawWindow;
        private ImageHandler _imgHandlerDraw;

        #pragma warning disable CS8618
        public MainApp() : base()
        {
            this._imgHandlerDraw = new ImageHandler();
            this.Startup += AppStartupHandler;
        }

        private void AppStartupHandler(object sender, StartupEventArgs e)
        {
            this.MainWindow = this._drawWindow = new DrawWindow(this._imgHandlerDraw);
            this.MainWindow.Show();
        }

    }
}
