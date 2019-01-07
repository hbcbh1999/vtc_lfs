using System;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace VTC.CaptureSource
{
    public class VideoFileCapture : CaptureSource
    {
        private readonly string _path;
        public double FrameCount;
        public double FrameRate;

        public VideoFileCapture(string path)
            : base("File: " + Path.GetFileName(path))
        {
            _path = path;
            var capture = new VideoCapture(_path);
            FrameCount = capture.GetCaptureProperty(CapProp.FrameCount);
            FrameRate = capture.GetCaptureProperty(CapProp.Fps);
        }

        protected override VideoCapture GetCapture()
        {
            var capture = new VideoCapture(_path);
            FrameCount = capture.GetCaptureProperty(CapProp.FrameCount);
            FrameRate = capture.GetCaptureProperty(CapProp.Fps);      
            return capture;
        }

       public override bool IsLiveCapture()
       {
            return false;
       }
    }
}
