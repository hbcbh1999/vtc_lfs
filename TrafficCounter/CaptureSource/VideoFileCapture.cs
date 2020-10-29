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
            var dateModified = File.GetLastWriteTimeUtc(_path);
            var selectedFileDate = dateModified;
            try
            {
                var encodedDateString =
                    mi.Get(MediaInfo.DotNetWrapper.Enumerations.StreamKind.Video, 0, "Encoded_Date");

                if (encodedDateString.Length >= 23)
                {
                    var trimmedEncodedDateString = encodedDateString.Substring(4);
                    selectedFileDate = DateTime.ParseExact(trimmedEncodedDateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                        DateTimeStyles.None);

                    if (selectedFileDate < dateModified)
                    {
                        _startDate = selectedFileDate;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            _startDate = selectedFileDate;

            try
            {
                _fps = fpsString.Length > 0 ? Double.Parse(fpsString,CultureInfo.InvariantCulture) : 24.0;
            }
            catch(FormatException ex)
            {
                _fps = 24.0;
            }

            try
            {
                _rotation = rotationString.Length > 0 ? Double.Parse(rotationString) : 0.0;
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
