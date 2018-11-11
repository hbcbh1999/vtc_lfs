using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Actors;

namespace VTC.Messages
{
    class CreateAllActorsMessage
    {
        public CreateAllActorsMessage(ProcessingActor.UpdateUIDelegate updateUiDelegate, LoggingActor.UpdateStatsUIDelegate statsUiDelegate, LoggingActor.UpdateInfoUIDelegate infoUiDelegate, FrameGrabActor.UpdateUIDelegate framegrabUiDelegate, LoggingActor.UpdateDebugDelegate debugDelegate)
        {
            UpdateUiDelegate = updateUiDelegate;
            UpdateStatsUiDelegate = statsUiDelegate;
            UpdateInfoUiDelegate = infoUiDelegate;
            UpdateFrameGrabUiDelegate = framegrabUiDelegate;
            UpdateDebugDelegate = debugDelegate;
        }

        public ProcessingActor.UpdateUIDelegate UpdateUiDelegate { get; private set; }
        public LoggingActor.UpdateStatsUIDelegate UpdateStatsUiDelegate { get; private set; }
        public LoggingActor.UpdateInfoUIDelegate UpdateInfoUiDelegate { get; private set; }
        public FrameGrabActor.UpdateUIDelegate UpdateFrameGrabUiDelegate { get; private set; }
        public LoggingActor.UpdateDebugDelegate UpdateDebugDelegate { get; private set;}
    }
}
