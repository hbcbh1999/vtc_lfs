using System;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;
using VTC.Common;
using NLog;

namespace VTC.CaptureSource
{
    public abstract class CaptureSource : Kernel.Video.ICaptureSource
    {
        private VideoCapture _cameraCapture;
        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        public int Width
        {
            get
            {
                if (_cameraCapture == null) throw new ApplicationException("Camera is not initialized.");
                return _cameraCapture.Width;
            }
        }

        public int Height
        {
            get
            {
                if (_cameraCapture == null) throw new ApplicationException("Camera is not initialized.");
                return _cameraCapture.Height;
            }
        }

        public string Name { get; }

        private bool _captureComplete;
        public bool CaptureComplete()
        {
            return _captureComplete;
        }

        protected abstract VideoCapture GetCapture();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Capture source name.</param>
        protected CaptureSource(string name)
        {
            Name = name;
        }

        public delegate void OnCaptureComplete();
        public OnCaptureComplete CaptureCompleteEvent;
        public OnCaptureComplete CaptureTerminatedEvent;

        /// <summary>
        /// Get next frame from camera or terminate capture
        /// </summary>
        /// <returns></returns>
        public Image<Bgr, Byte> QueryFrame()
        {
            try
            {
                if (_cameraCapture != null)
                {
                    {
                        var frame = _cameraCapture.QueryFrame();
                        if (frame == null)
                        {
                            if (_cameraCapture.CaptureSource == VideoCapture.CaptureModuleType.Camera)
                            {
                                _captureComplete = true;
                                CaptureTerminatedEvent?.Invoke();
                            }
                            else
                            {
                                _captureComplete = true;
                                CaptureCompleteEvent?.Invoke();
                            }

                            return null;
                        }

                        return frame.ToImage<Bgr, byte>().Resize(640, 480, Inter.Cubic);
                    }
                }
            }
            catch (NullReferenceException e)
            {
                Logger.Log(LogLevel.Error, e);
            }
            catch (AccessViolationException e)
            {
                Logger.Log(LogLevel.Error, e);
            }
            
            return null;
        }

        /// <summary>
        /// Initialize camera.
        /// </summary>
        /// <param name="settings"></param>
        public void Init()
        {
            _cameraCapture = GetCapture();
        }

        /// <summary>
        /// Destroy underlying camera.
        /// </summary>
        public void Destroy()
        {
            if (null != _cameraCapture)
            {
                _cameraCapture.Dispose();
                _cameraCapture = null;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
