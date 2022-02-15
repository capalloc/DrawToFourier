using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawToFourier.Fourier
{
    public class FourierCore
    {
        public class ProgramActionEventArgs : EventArgs
        {
            public string? ActionName { get; set; }

            public ProgramActionEventArgs(string? actionName)
            {
                this.ActionName = actionName;
            }
        }

        public delegate void ProgramActionEventHandler(object sender, ProgramActionEventArgs e);

        public FourierCore()
        {

        }

        public void OnProgramAction(object sender, ProgramActionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.ActionName);
        }
    }
}
