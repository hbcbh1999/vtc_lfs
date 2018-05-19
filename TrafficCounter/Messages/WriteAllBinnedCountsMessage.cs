using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;

namespace VTC.Messages
{
    class WriteAllBinnedCountsMessage
    {
        public WriteAllBinnedCountsMessage(double timestep)
        {
            Timestep = timestep;
        }

        public double Timestep { get; private set; }
    }
}
