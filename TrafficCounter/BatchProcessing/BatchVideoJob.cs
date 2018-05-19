using System;
using VTC.Common.RegionConfig;

namespace VTC.BatchProcessing
{
    public class BatchVideoJob
    {
        public string VideoPath;
        public RegionConfig RegionConfiguration;
        public string GroundTruthPath;
        public Guid JobGuid;
    }
}
