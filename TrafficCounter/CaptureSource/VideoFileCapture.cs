using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
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

            var mi = new MediaInfo.DotNetWrapper.MediaInfo();
            mi.Open(_path);
            var s = mi.Inform();
            Console.WriteLine(s);
            var fpsString = mi.Get(MediaInfo.DotNetWrapper.Enumerations.StreamKind.Video,0,"FrameRate");
            var rotationString = mi.Get(MediaInfo.DotNetWrapper.Enumerations.StreamKind.Video,0,"Rotation");
            var encodedDateString = mi.Get(MediaInfo.DotNetWrapper.Enumerations.StreamKind.Video, 0, "Encoded_Date");
            var trimmedEncodedDateString = encodedDateString.Substring(4);

            try
            {
                _startDate = DateTime.ParseExact(trimmedEncodedDateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
            }
            catch (FormatException ex)
            {
                _startDate = DateTime.Now;
            }
            
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

        public override DateTime StartDateTime()
        {
            return _startDate;
        }

        public override double Rotation()
        { 
            return _rotation;    
        }
    }
}
