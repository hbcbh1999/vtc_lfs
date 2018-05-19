using System;
using System.Threading;

namespace VTC.Reporting
{

    public abstract class ReportItem
    {
        private readonly int _reportIntervalMinutes;

        protected ReportItem(int reportIntervalMinutes)
        {
            _reportIntervalMinutes = reportIntervalMinutes;
        }

        public void ReportIfIntervalUp(long runTimeMinutes)
        {
            if (runTimeMinutes % _reportIntervalMinutes == 0)
            {
                // Interval hit
                Report();
            }
        }

        protected abstract void Report();
    }
}
