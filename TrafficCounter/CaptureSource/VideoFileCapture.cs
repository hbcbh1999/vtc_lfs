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

        public VideoFileCapture(string path)
            : base("File: " + Path.GetFileName(path))
        {
            _path = path;
            var capture = new VideoCapture(_path);
            FrameCount = capture.GetCaptureProperty(CapProp.FrameCount);
            //FrameRate = capture.GetCaptureProperty(CapProp.Fps);

            var mi = new MediaInfo.DotNetWrapper.MediaInfo();
            mi.Open(_path);
            var s = mi.Inform();
            Console.WriteLine(s);
            //mi.Option("Info_Parameters");
            var fpsString = mi.Get(MediaInfo.DotNetWrapper.Enumerations.StreamKind.Video,0,"FrameRate");
            var rotationString = mi.Get(MediaInfo.DotNetWrapper.Enumerations.StreamKind.Video,0,"Rotation");

            try
            {
                _fps = Double.Parse(fpsString);
            }
            catch(FormatException ex)
            {
                _fps = 24.0;
            }

            try
            {
                _rotation = Double.Parse(rotationString);
            }
            catch (FormatException ex)
            {
                _rotation = 0.0;
            }

        }

        protected override VideoCapture GetCapture()
        {
            var capture = new VideoCapture(_path);
            FrameCount = capture.GetCaptureProperty(CapProp.FrameCount);
            //FrameRate = capture.GetCaptureProperty(CapProp.Fps);      
            return capture;
        }

        public override bool IsLiveCapture()
        {
            return false;
        }

        public override double FPS()
        {
            return _fps;
        }

        public override double Rotation()
        { 
            return _rotation;    
        }
    }
}
