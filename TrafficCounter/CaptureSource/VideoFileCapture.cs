using System;
using System.IO;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;
using MediaInfo.DotNetWrapper;


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

        public override double FPS()
        {
            var mi = new MediaInfo.DotNetWrapper.MediaInfo();
            mi.Open(_path);
            //mi.Option("Info_Parameters");
            var fpsString = mi.Get(MediaInfo.DotNetWrapper.Enumerations.StreamKind.Video,0,"FPS");
            var fps = Double.Parse(fpsString);
            return fps;
        }
    }
}
