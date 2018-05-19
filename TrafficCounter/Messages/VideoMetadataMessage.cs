using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using VTC.Common;

namespace VTC.Messages
{
    class VideoMetadataMessage
    {
        public VideoMetadataMessage(VideoMetadata vm)
        {
            VM = vm;
        }

        public VideoMetadata VM { get; private set; }
    }
}
