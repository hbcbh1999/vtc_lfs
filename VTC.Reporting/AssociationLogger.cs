using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;
using VTC.Common.RegionConfig;

namespace VTC.Reporting
{
    public class AssociationLogger
    {
        private readonly RegionConfig _regionConfig;
        public readonly Dictionary<Measurement, TrackedObject> Associations;

        public AssociationLogger(RegionConfig regionConfig, Dictionary<Measurement, TrackedObject> associations)
        {
            _regionConfig = regionConfig;
            Associations = associations;
        }

        private string MeasurementToString(Measurement m)
        {
            var s = "";
            s += "X: " + Math.Round(m.X, 1);
            s += " Y: " + Math.Round(m.Y, 1);
            s += " R: " + Math.Round(m.Red, 1);
            s += " G: " + Math.Round(m.Green, 1);
            s += " B: " + Math.Round(m.Blue, 1);
            s += " Class: " + m.ObjectClass;
            s += " Size: " + Math.Round(m.Size, 1);
            s += " Width: " + Math.Round(m.Width, 1);
            s += " Height: " + Math.Round(m.Height, 1);
            return s;
        }

        private string TrackedObjectToString(TrackedObject tobject)
        {
            var s = "";
            s += "X: " + Math.Round(tobject.StateHistory.Last().X, 1);
            s += " Y: " + Math.Round(tobject.StateHistory.Last().Y, 1);
            s += " VX: " + Math.Round(tobject.StateHistory.Last().Vx, 1);
            s += " VY: " + Math.Round(tobject.StateHistory.Last().Vy, 1);
            s += " R: " + Math.Round(tobject.StateHistory.Last().Red, 1);
            s += " G: " + Math.Round(tobject.StateHistory.Last().Green, 1);
            s += " B: " + Math.Round(tobject.StateHistory.Last().Blue, 1);
            s += " Class: " + tobject.StateHistory.Last().MostFrequentClassId();
            s += " Size: " + Math.Round(tobject.StateHistory.Last().Size, 1);
            
            s += " CovX: " + Math.Round(tobject.StateHistory.Last().CovX, 1);
            s += " CovY: " + Math.Round(tobject.StateHistory.Last().CovY, 1);
            s += " CovVX: " + Math.Round(tobject.StateHistory.Last().CovVx, 1);
            s += " CovVY: " + Math.Round(tobject.StateHistory.Last().CovVy, 1);
            s += " CovR: " + Math.Round(tobject.StateHistory.Last().CovRed, 1);
            s += " CovG: " + Math.Round(tobject.StateHistory.Last().CovGreen, 1);
            s += " CovB: " + Math.Round(tobject.StateHistory.Last().CovBlue, 1);
            s += " CovSize: " + Math.Round(tobject.StateHistory.Last().CovSize, 1);
            return s;
        }

        private string AssociationToString(Measurement measurement, TrackedObject tobject)
        {
            var s = "";

            s += "Measurement: ["+ MeasurementToString(measurement) + "], ";
            s += "TrackedObject: [" + TrackedObjectToString(tobject) + "]";

            return s;
        }

        public void LogToTextfile(string textfilePath)
        {
            try
            {
                var logString = "";
                foreach (KeyValuePair<Measurement, TrackedObject> kp in Associations)
                {
                    logString += "[" + AssociationToString(kp.Key, kp.Value) + "], ";
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
