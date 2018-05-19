using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Schema;

namespace VTC.Common.RegionConfig
{ 
    [DataContract]
    public class RegionConfig
    {
        [DataMember]
        public Polygon RoiMask;

        [DataMember]
        public readonly Guid ConfigGuid;

        [DataMember]
        public Dictionary<string, Polygon> Regions;

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string FTPpassword { get; set; } = "";

        [DataMember]
        public string FTPusername { get; set; } = "";

        [DataMember]
        public string ServerURL { get; set; } = "";

        [DataMember]
        public int FrameUploadIntervalMinutes { get; set; } = 5;

        [DataMember]
        public int StateUploadIntervalMs { get; set; } = 2000;

        [DataMember]
        public string IntersectionId { get; set; } = "1";

        [DataMember]
        public double Q_position { get; set; } = 500;

        [DataMember]
        public double Q_color { get; set; } = 100000;

        [DataMember]
        public double R_position { get; set; } = 15;

        [DataMember]
        public double R_color { get; set; } = 100000;

        [DataMember]
        public double VehicleInitialCovX { get; set; } = 1000;

        [DataMember]
        public double VehicleInitialCovVX { get; set; } = 5000;

        [DataMember]
        public double VehicleInitialCovY { get; set; } = 1000;

        [DataMember]
        public double VehicleInitialCovVY { get; set; } = 5000;

        [DataMember]
        public double VehicleInitialCovR { get; set; } = 100000;

        [DataMember]
        public double VehicleInitialCovG { get; set; } = 100000;

        [DataMember]
        public double VehicleInitialCovB { get; set; } = 100000;

        [DataMember]
        public double CompensationGain { get; set; } = 200;

        [DataMember]
        public double Timestep { get; set; } = 0.1;

        [DataMember]
        public int MinObjectSize { get; set; } = 200;

        [DataMember]
        public int MissThreshold { get; set; } = 10;

        [DataMember]
        public double LambdaF { get; set; } = 4E-07;

        [DataMember]
        public double LambdaN { get; set; } = 5E-07;

        [DataMember]
        public double Pd { get; set; } = 0.8;

        [DataMember]
        public double Px { get; set; } = 0.0001;

        [DataMember]
        public double VehicleInitialCovSize { get; set; } = 10000000;

        [DataMember]
        public double Q_size { get; set; } = 10000000;

        [DataMember]
        public double R_size { get; set; } = 10000000;

        [DataMember]
        public bool PushToServer { get; set; } = false;

        [DataMember]
        public int MinPathLength { get; set; } = 100;

        [DataMember]
        public int MaxHypTreeDepth { get; set; } = 5;

        [DataMember]
        public int MaxTargets { get; set; } = 6;

        [DataMember]
        public int KHypotheses { get; set; } = 4;

        [DataMember]
        public int ValRegDeviation { get; set; } = 6;

        [DataMember]
        public int MaxObjectCount { get; set; } = 6;

        public RegionConfig()
        {
            RoiMask = new Polygon();
            Regions = new Dictionary<string, Polygon>
            {
                {"Approach 1", new Polygon()},
                {"Approach 2", new Polygon()},
                {"Approach 3", new Polygon()},
                {"Approach 4", new Polygon()},
                {"Exit 1", new Polygon()},
                {"Exit 2", new Polygon()},
                {"Exit 3", new Polygon()},
                {"Exit 4", new Polygon()},
                {"Sidewalk 1", new Polygon(true)},
                {"Sidewalk 2", new Polygon(true)},
                {"Sidewalk 3", new Polygon(true)},
                {"Sidewalk 4", new Polygon(true)}
            };

            ConfigGuid = Guid.NewGuid();
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var rObj = (RegionConfig) obj;

            if (rObj.ConfigGuid != ConfigGuid)
            {
                return false;
            }

            return true;
        }

    }
}
