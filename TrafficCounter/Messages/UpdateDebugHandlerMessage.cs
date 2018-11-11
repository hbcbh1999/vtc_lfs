using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Actors;
using VTC.Common.RegionConfig;

namespace VTC.Messages
{
    class UpdateDebugHandlerMessage
    {
        public UpdateDebugHandlerMessage(LoggingActor.UpdateDebugDelegate debugDelegate)
        {
            DebugDelegate = debugDelegate;
        }

        public LoggingActor.UpdateDebugDelegate DebugDelegate { get; private set; }
    }
}
