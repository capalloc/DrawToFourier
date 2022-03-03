using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawToFourier.UI
{
    public class DrawAreaSize
    {
        public int InitialHeight 
        { 
            get 
            {
                return this._initialHeight;
            } 
            private set
            {
                this._initialHeight = value;
            } 
        }

        public int InitialWidth 
        { 
            get
            {
                return this._initialWidth;
            }
            private set
            {
                this._initialWidth = value;
            }
        }

        public int DesiredHeight {
            get 
            {
                return this._desiredHeight;
            }
            set 
            {
                this._desiredHeight = value;
            } 
        }

        public int DesiredWidth
        {
            get
            {
                return this._desiredWidth;
            }
            set
            {
                this._desiredWidth = value;
            }
        }

        public int Height
        {
            get
            {
                return this._height;
            }
            private set
            {
                this._height = value;
            }
        }

        public int Width
        {
            get
            {
                return this._width;
            }
            private set
            {
                this._width = value;
            }
        }

        private int _initialHeight;
        private int _initialWidth;
        private int _desiredHeight;
        private int _desiredWidth;
        private int _height;
        private int _width;

        public DrawAreaSize(int initialWidth, int initialHeight)
        {
            this.InitialWidth = this.DesiredWidth = initialWidth;
            this.InitialHeight = this.DesiredHeight = initialHeight;
            this.Height = this.Width = Math.Min(initialWidth, initialHeight);
        }

        public void NewSize(int desiredWidth, int desiredHeight)
        {
            this.DesiredWidth = desiredWidth;
            this.DesiredHeight = desiredHeight;
            this.Height = this.Width = Math.Min(desiredWidth, desiredHeight);
        }
    }
}
