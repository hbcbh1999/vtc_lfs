using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;
using VTC.Common.RegionConfig;
using System.Runtime.Serialization;

namespace VTC.Reporting
{
    public class DetectionLogger
    {
        private readonly MeasurementList _measurements = new MeasurementList();

        public DetectionLogger(List<Measurement> measurements)
        {
            _measurements.Measurements = measurements.Select(RoundForLogging).ToList();
        }

        public void LogToJsonfile(string filePath)
        {
            try
            {
                var logString = JsonLogger<MeasurementList>.ToJsonLogString(_measurements);

                string filepathWithExtension = filePath + ".json";
                if (!File.Exists(filepathWithExtension))
                    File.Create(filepathWithExtension);

                using (var sw = new StreamWriter(filepathWithExtension, true))
                    sw.WriteLine(logString);
            }
            catch (Exception e)
            {
                Debug.WriteLine("LogToJsonfile:" + e.Message);
            }
        }

        private static Measurement RoundForLogging(Measurement m)
        {
            m.X = Math.Round(m.X, 1);
            m.Y = Math.Round(m.Y, 1);
            m.Blue = Math.Round(m.Blue, 0);
            m.Red = Math.Round(m.Red, 0);
            m.Green = Math.Round(m.Green, 0);
            m.Height = Math.Round(m.Height, 1);
            m.Width = Math.Round(m.Width, 0);
            m.Size = Math.Round(m.Size, 0);
            return m;
        }

    }

    //This class needs to be defined in order to implement the DataContract attribute, which allows JsonLogger to log the object.
    [DataContract]
    public class MeasurementList
    {
        [DataMember]
        public List<Measurement> Measurements;
    }
}
