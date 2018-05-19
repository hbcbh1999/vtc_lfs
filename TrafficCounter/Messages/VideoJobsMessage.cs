using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.BatchProcessing;

namespace VTC.Messages
{
    class VideoJobsMessage
    {
        public VideoJobsMessage(List<BatchVideoJob> videoJobs)
        {
            VideoJobsList = videoJobs;
        }

        public List<BatchVideoJob> VideoJobsList { get; private set; }
    }
}
