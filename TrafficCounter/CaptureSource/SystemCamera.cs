using Emgu.CV;

namespace VTC.CaptureSource
{
    public class SystemCamera : CaptureSource
    {
        private readonly int _systemDeviceIndex;

        public SystemCamera(string name, int systemDeviceIndex) : base(name)
        {
            _systemDeviceIndex = systemDeviceIndex;
        }

        protected override VideoCapture GetCapture()
        {
            return new VideoCapture(_systemDeviceIndex);
        }
    }
}
