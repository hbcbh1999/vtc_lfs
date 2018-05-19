using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;

namespace VTC.Messages
{
    class LogDetectionsMessage
    {
        public readonly List<Measurement> Detections;

        public LogDetectionsMessage(List<Measurement> detections)
        {
            Detections = detections;
        }
    }
}
