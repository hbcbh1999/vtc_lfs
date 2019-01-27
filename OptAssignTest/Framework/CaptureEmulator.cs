using System;
using System.Drawing;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using VTC.Kernel.Video;
using VTC.Common;

namespace OptAssignTest.Framework
{
    class CaptureEmulator : ICaptureSource
    {
        private readonly Script _script;
        private Image<Bgr, byte> _background;
        private uint _frame;

        public string Name { get; }

        public int Width
        {
            get
            {
                return 640;
            }
        }

        public int Height
        {
            get { return 480; }
        }

        public Image<Bgr, byte> QueryFrame()
        {

            Image<Bgr, byte> image;
            if (_frame == 0)
            {
                image = _background.Clone(); // check - should it be cloned?
            }
            else
            {
                image = _background.Clone();
                _script.Draw(_frame, image);
            }

            // start script again
            if (_script.IsDone(_frame))
            {
                _frame = 0;
            }
            else
            {
                _frame++;
            }

            Thread.Sleep(0);

            return image;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Capture name.</param>
        /// <param name="script">Underlying script.</param>
        public CaptureEmulator(string name, Script script)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (script == null) throw new ArgumentNullException(nameof(script));

            Name = name;
            _script = script;
        }

        public void Init()
        {
            _background = new Image<Bgr, byte>((int) 640, (int) 480, new Bgr(Color.Black));
            _frame = 0;
        }

        public void Destroy()
        {
        }

        public bool CaptureComplete()
        {
            return false;
        }

        public bool IsLiveCapture()
        {
            return false;
        }

        public double FPS()
        {
            return 10.0;
        }
    }
}
