using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;

namespace VTC.Messages
{
    class LogAssociationsMessage
    {
        public readonly Dictionary<Measurement,TrackedObject> Associations;

        public LogAssociationsMessage(Dictionary<Measurement,TrackedObject> associations)
        {
            Associations = associations;
        }
    }
}
