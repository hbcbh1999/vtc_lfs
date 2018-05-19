using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace VTC.Messages
{
    class LogMessage : StringMessage
    {
        public LogMessage(string text, LogLevel level) : base(text)
        {
            Level = level;
        }

        public LogLevel Level { get; private set; }
    }
}
