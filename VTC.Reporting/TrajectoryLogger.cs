using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Remote;

namespace VTC.Reporting
{
    public class TrajectoryLogger
    {
        private readonly Movement _movement;
        private RemoteServer _remoteServer;

        public TrajectoryLogger(Movement movement)
        {          
            _movement = RoundForLogging(movement);
        }

        public void Save(string filePath)
        {
            try
            {
                var logString = JsonLogger<Movement>.ToJsonLogString(_movement);
                string pathWithExtension = filePath + ".json";
                using (var sw = File.AppendText(pathWithExtension))
                {
                    sw.WriteLine(logString);
                }                
            }
            catch (Exception e)
            {
                Debug.WriteLine("LogToJsonfile:" + e.Message);
            }   
        }

        private Movement RoundForLogging(Movement m)
        {
            var stateEstimatesRounded = new StateEstimateList();
            stateEstimatesRounded.AddRange(m.StateEstimates.Select(RoundForLogging));
            m.StateEstimates = stateEstimatesRounded;
            return m;
        }

        private StateEstimate RoundForLogging(StateEstimate s)
        {
            s.X = Math.Round(s.X, 1);
            s.Y = Math.Round(s.Y, 1);
            s.Blue = Math.Round(s.Blue, 0);
            s.Red = Math.Round(s.Red, 0);
            s.Green = Math.Round(s.Green, 0);
            s.CovBlue = Math.Round(s.CovBlue, 0);
            s.CovGreen = Math.Round(s.CovGreen, 0);
            s.CovRed = Math.Round(s.CovRed, 0);
            s.Vx = Math.Round(s.Vx, 1);
            s.Vy = Math.Round(s.Vy, 1);
            s.CovX = Math.Round(s.CovX, 0);
            s.CovY = Math.Round(s.CovY, 0);
            s.CovVx = Math.Round(s.CovVx, 0);
            s.CovVy = Math.Round(s.CovVy, 0);
            s.Size = Math.Round(s.Size, 0);
            s.CovSize = Math.Round(s.CovSize, 0);
            s.PathLength = Math.Round(s.PathLength, 0);
            return s;
        }
    }
}
