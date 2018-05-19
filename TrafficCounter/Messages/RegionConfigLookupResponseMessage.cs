using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common.RegionConfig;

namespace VTC.Messages
{
    class RegionConfigLookupResponseMessage
    {
        public RegionConfigLookupResponseMessage(RegionConfig config, Guid jobGuid)
        {
            Configuration = config;
            JobGuid = jobGuid;
        }

        public Guid JobGuid;
        public RegionConfig Configuration;
    }
}
