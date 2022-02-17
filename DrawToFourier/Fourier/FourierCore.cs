using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawToFourier.Fourier
{
    // Business logic of the program, i.e. computations directly related to Fourier resides here.
    public class FourierCore
    {
        // This class communicates with the UI by getting notified through its delegates, which should be passed to the UI classes and subscribed to respective events
        public class CoreProgramActionEventArgs : EventArgs
        {
            public string? ActionName { get; set; }
            public double? X { get; set; }
            public double? Y { get; set; }

            public CoreProgramActionEventArgs(string? actionName)
            {
                this.ActionName = actionName;
            }

            public CoreProgramActionEventArgs(string? actionName, double x, double y)
            {
                this.ActionName = actionName;
                this.X = x;
                this.Y = y;
            }
        }

        public delegate void CoreProgramActionEventHandler(object sender, CoreProgramActionEventArgs e);

        public FourierCore()
        {

        }

        public void OnUIAction(object sender, CoreProgramActionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.ActionName);
        }

        public void OnPathAction(object sender, CoreProgramActionEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine($"{e.ActionName} {e.X} {e.Y}");
        }
    }
}
