using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
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

        [Description("The name of this configuration")]
        [DataMember]
        public string Title { get; set; }

        [Description("Q_position: position movement covariance. Decrease for objects with smooth movement; increase for objects with complex movement.")]
        [DataMember]
        public double Q_position { get; set; } = 300;

        [Description("Q_color: color-change covariance. Decrease for scenes with consistent lighting; increase for scenes with variable lighting.")]
        [DataMember]
        public double Q_color { get; set; } = 100000;

        [Description("R_position: position measurement covariance. Decrease when detection-quality is high. Increase when detection-quality is low.")]
        [DataMember]
        public double R_position { get; set; } = 25;

        [Description("R_color: color measurement covariance. Decrease for scenes with consistent lighting; increase for scenes with variable lighting.")]
        [DataMember]
        public double R_color { get; set; } = 100000;

        [Description("Initial X covariance: increase for scenes with nearby vehicles. Decrease for scenes with far-away vehicles.")]
        [DataMember]
        public double VehicleInitialCovX { get; set; } = 1000;

        [Description("Initial Vx covariance: increase for scenes with rapidly-moving vehicles. Decrease for scenes with slowly-moving vehicles.")]
        [DataMember]
        public double VehicleInitialCovVX { get; set; } = 5000;

        [Description("Initial Y covariance: increase for scenes with nearby vehicles. Decrease for scenes with far-away vehicles.")]
        [DataMember]
        public double VehicleInitialCovY { get; set; } = 1000;

        [Description("Initial Vy covariance: increase for scenes with rapidly-moving vehicles. Decrease for scenes with slowly-moving vehicles.")]
        [DataMember]
        public double VehicleInitialCovVY { get; set; } = 5000;

        [Description("Initial R covariance: initial assumption on R-channel color certainty.")]
        [DataMember]
        public double VehicleInitialCovR { get; set; } = 100000;

        [Description("Initial G covariance: initial assumption on G-channel color certainty.")]
        [DataMember]
        public double VehicleInitialCovG { get; set; } = 100000;

        [Description("Initial B covariance: initial assumption on B-channel color certainty.")]
        [DataMember]
        public double VehicleInitialCovB { get; set; } = 100000;

        [Description("Compensation gain: how aggressively the tracker seeks a lost object.")]
        [DataMember]
        public double CompensationGain { get; set; } = 200;

        [Description("Timestep: the assumed change in time between frames (i.e. 1/framerate).")]
        [DataMember]
        public double Timestep { get; set; } = 0.1;

        [Description("MinObjectSize: objects with less than this many pixels will be ignored.")]
        [DataMember]
        public int MinObjectSize { get; set; } = 200;

        [Description("MissThreshold: if an object is not detected for this many frames, it will be considered to be gone.")]
        [DataMember]
        public int MissThreshold { get; set; } = 10;

        [Description("Lambda-F: The odds of a false-positive object detection per frame.")]
        [DataMember]
        public double LambdaF { get; set; } = 4E-07;

        [Description("Lambda-N: The odds of a new object detection per frame.")]
        [DataMember]
        public double LambdaN { get; set; } = 5E-07;

        [Description("Pd: probability of detecting a particular vehicle.")]
        [DataMember]
        public double Pd { get; set; } = 0.8;

        [Description("Px: probability of a particular vehicle exiting the scene in any frame.")]
        [DataMember]
        public double Px { get; set; } = 0.0001;

        [Description("VehicleInitialCovSize: the uncertainty in initial object size. Increase if large object tracking-quality is poor.")]
        [DataMember]
        public double VehicleInitialCovSize { get; set; } = 10000000;

        [Description("Q_size: object size change. Increase for scenes where objects appear to change in size significantly, i.e. for fish-eye or wide-angle lenses.")]
        [DataMember]
        public double Q_size { get; set; } = 10000000;

        [Description("R_size: size measurement uncertainty. Increase if objects are being 'lost' by trackers. Decrease if trackers are swapping.")]
        [DataMember]
        public double R_size { get; set; } = 10000000;

        [Description("MinPathLength: Object trajectories shorter than this length (in pixels) are discarded without being counted.")]
        [DataMember]
        public int MinPathLength { get; set; } = 200;

        [Description("MHT maximum hypothesis tree-depth: how many frames of tracker history are retained. Increase for better quality; decrease for faster processing.")]
        [DataMember]
        public int MaxHypTreeDepth { get; set; } = 5;

        [Description("Maximum number of tracked objects.")]
        [DataMember]
        public int MaxTargets { get; set; } = 6;

        [Description("K-hypotheses (MHT branch factor): how many association-hypotheses are generated per timestep. Increase for better quality; decrease for faster processing.")]
        [DataMember]
        public int KHypotheses { get; set; } = 4;

        [Description("Validation-region deviation: The number of Mahalanobis-distance deviations allowable from the predicted detection location, used in association-gating. Reduce if trackers are swapping. Increase if trackers are falling behind objects.")]
        [DataMember]
        public int ValRegDeviation { get; set; } = 4;

        [Description("Maximum object count: The number of objects which may be detected in a single frame. This value should be the same as MaxTargets.")]
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
