using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTC.Messages
{
    class RegionConfigNameLookupMessage
    {
        public RegionConfigNameLookupMessage(string regionConfigName, Guid jobGuid)
        {
            RegionConfigName = regionConfigName;
            JobGuid = jobGuid;
        }

        public string RegionConfigName;
        public Guid JobGuid;
    }
}
