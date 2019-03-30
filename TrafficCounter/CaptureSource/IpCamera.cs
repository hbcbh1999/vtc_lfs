using Emgu.CV;

namespace VTC.CaptureSource
{
    public class IpCamera : CaptureSource
    {
        public readonly string ConnectionString;

        public IpCamera(string name, string connectionString) : base(name)
        {
            ConnectionString = connectionString;
        }

        protected override VideoCapture GetCapture()
        {
            return new VideoCapture(ConnectionString);
        }

       public override bool IsLiveCapture()
       {
            return true;
       }

        public override double FPS()
        {
            return _fps;
        }
    }
}
