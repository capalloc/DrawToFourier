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

            public CoreProgramActionEventArgs(string? actionName)
            {
                this.ActionName = actionName;
            }
        }

        public delegate void CoreProgramActionEventHandler(object sender, CoreProgramActionEventArgs e);

        public Path ActivePath { get; }

        private LinkedList<Path> completedPaths;
        private Path activePath;
        
        #pragma warning disable CS8618
        public FourierCore()
        {
            this.completedPaths = new LinkedList<Path>();
            this.activePath = new Path();
        }

        public void OnUIAction(object sender, CoreProgramActionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.ActionName);
        }

        
    }
}
