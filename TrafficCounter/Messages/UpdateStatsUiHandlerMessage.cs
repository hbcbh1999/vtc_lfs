using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Actors;
using VTC.Common.RegionConfig;

namespace VTC.Messages
{
    class UpdateStatsUiHandlerMessage
    {
        public UpdateStatsUiHandlerMessage(LoggingActor.UpdateStatsUIDelegate uiDelegate)
        {
            StatsUiDelegate = uiDelegate;
        }

        public LoggingActor.UpdateStatsUIDelegate StatsUiDelegate { get; private set; }
    }
}
