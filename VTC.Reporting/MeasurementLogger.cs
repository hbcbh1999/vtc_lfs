using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VTC.Common;
using VTC.Common.RegionConfig;

namespace VTC.Reporting
{
    public class MeasurementLogger
    {
        private readonly RegionConfig _regionConfig;

        //Data values
        private readonly List<TrackedObject> _currentIntersectionState;
        private readonly DateTime _time;

        public MeasurementLogger(RegionConfig regionConfig, List<TrackedObject> currentState)
        {
            _regionConfig = regionConfig;
            _currentIntersectionState = currentState;
            _time = DateTime.Now;
        }

        public MeasurementLogger(RegionConfig regionConfig, List<TrackedObject> currentState, DateTime time)
        {
            _regionConfig = regionConfig;
            _currentIntersectionState = currentState;
            _time = time;
        }

        public void LogToTextfile(string textfilePath)
        {
            try
            {
                var logString = "";
                logString += _time.ToString("yyyy-MM-ddThh:mm:ssss%K ");

                if (_currentIntersectionState != null)
                {
                    logString += "[";

                    logString = _currentIntersectionState.Aggregate(logString, (current, s) =>
                    {
                        if (s.ObjectType != "unknown" && s.ObjectType != "garbage")
                        {
                            return current +
                                    $"(x:{Math.Round(s.StateHistory.Last().X, 0)},y:{Math.Round(s.StateHistory.Last().Y, 0)},vx:{Math.Round(s.StateHistory.Last().Vx, 1)},vy:{Math.Round(s.StateHistory.Last().Vy, 1)},R:{Math.Round(s.StateHistory.Last().Red, 0)},G:{Math.Round(s.StateHistory.Last().Green, 0)},B:{Math.Round(s.StateHistory.Last().Blue, 0)},size:{Math.Round(s.StateHistory.Last().Size, 0)},type:{s.ObjectType})";
                        }
                        else
                            return current;
                    });

                    logString += "]";
                }

                string filepath = textfilePath + ".txt";
                if (!File.Exists(filepath))
                    File.Create(filepath);

                using (var sw = new StreamWriter(filepath, true))
                    sw.WriteLine(logString);
            }
            catch (Exception e)
            {
                Debug.WriteLine("LogToTextfile:" + e.Message);
            }

        }
    }
}
