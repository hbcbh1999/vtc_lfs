using Emgu.CV;

namespace VTC.CaptureSource
{
    public class IpCamera : CaptureSource
    {
        private readonly string _connectionString;

        public IpCamera(string name, string connectionString) : base(name)
        {
            _connectionString = connectionString;
        }

        protected override VideoCapture GetCapture()
        {
            return new VideoCapture(_connectionString);
        }

       public override bool IsLiveCapture()
       {
            return true;
       }

        public override double FPS()
        {
            return _calculatedFPS;
        }
    }
}
