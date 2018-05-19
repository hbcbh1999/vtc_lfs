using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.Structure;

namespace VTC.Messages
{
    class ProcessNextFrameMessage : FrameMessage
    {
        public ProcessNextFrameMessage(Image<Bgr, byte> frame) : base(frame)
        {
        }
    }
}
