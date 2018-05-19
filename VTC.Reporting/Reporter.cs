using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace VTC.Reporting
{
    public class Reporter
    {
        private Timer _reportTimer;
        private readonly List<ReportItem> _reportItems;

        // Reporter is a singleton as we don't want to have multiple instances all
        // reporting back on their own
        private static Reporter _instance;
        public static Reporter Instance => _instance ?? (_instance = new Reporter());

        private Reporter()
        {
            _reportItems = new List<ReportItem>();
        }

        public void Start()
        {
            // Stop existing instance
            Stop();

            // Set up timer to tick every minute - different ReportItems can tick at different minute intervals
            // eg. every 1, 5, 10, etc minutes.  
            _runTimeMinutes = 0;
            _reportTimer = new Timer {Interval = 60*1000};
            _reportTimer.Tick += ReportTimer_Tick;
            _reportTimer.Start();
        }

        public void Stop()
        {
            if (null != _reportTimer)
            {
                if (_reportTimer.Enabled)
                {
                    _reportTimer.Stop();
                }
            }
        }

        private static long _runTimeMinutes;
        private void ReportTimer_Tick(object sender, EventArgs e)
        {
            _runTimeMinutes++;
            foreach (var reportItem in _reportItems)
            {
                try
                {
                    reportItem.ReportIfIntervalUp(_runTimeMinutes);
                }
                catch (Exception ex)
                {
                    #if(DEBUG)
                    {
                        throw;
                    }
                    #else
                    {
                        Trace.WriteLine(ex.Message);
                        Trace.WriteLine(ex.InnerException);
                        Trace.WriteLine(ex.StackTrace);
                        Trace.WriteLine(ex.TargetSite);
                    }
                    #endif
                }
            }
        }

        public void AddReportItem(ReportItem item)
        {
            _reportItems.Add(item);
        }

        public void RemoveReportItem(ReportItem item)
        {
            _reportItems.Remove(item);
        }
    }
}
