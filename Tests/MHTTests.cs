using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VTC.Kernel;
using VTC.Common.RegionConfig;
using VTC.Common;

namespace Tests
{
    [TestClass]
    public class MHTTests
    {

        [TestMethod]
        public void NoObjectsTest()
        {
            var sg = new SceneGenerator();
            var rc = new RegionConfig();
            var vc = new VelocityField(640, 480);
            var mht = new MultipleHypothesisTracker(rc, vc);
            var timestep = 0.1;
            var maxFrame = 10;
            var ct = new System.Threading.CancellationToken();
            
            for(int i=0;i<maxFrame;i++)
            {   
                var detections = sg.GetNextFrame();
                mht.Update(detections.ToArray(), ct, timestep);

                Assert.AreEqual(mht.MostLikelyStateHypothesis().Vehicles.Count, 0);
            }
        }

        [TestMethod]
        public void SingleObjectsTest()
        {
            var sg = new SceneGenerator();
            sg.CreateObject(0,0,640,480);

            var rc = new RegionConfig();
            rc.KHypotheses = 4;
            rc.MaxHypTreeDepth = 4;
            var vc = new VelocityField(640, 480);
            var mht = new MultipleHypothesisTracker(rc, vc);
            var timestep = 0.1;
            var maxFrame = 100;
            
            var ct = new System.Threading.CancellationToken();
            var totalDetectionCount = 0;

            var DetectionsByFrame = new Dictionary<int,List<Measurement>>();
            var StateHypothesisList = new Dictionary<int, StateHypothesis>();

            for (int i = 0; i < maxFrame; i++)
            {
                var detections = sg.GetNextFrame();
                DetectionsByFrame.Add(i,detections);
                mht.Update(detections.ToArray(), ct, timestep);
                var hypothesis = mht.MostLikelyStateHypothesis();
                StateHypothesisList.Add(i,hypothesis);
                var trackedVehicleCount = hypothesis.Vehicles.Count;
                totalDetectionCount += detections.Count;

                if (i > 10)
                {
                    var averageDetections = (double) totalDetectionCount / (i+1);
                    var expectedCount = sg.SceneObjects.Count;
                    var countError = Math.Abs(trackedVehicleCount - expectedCount);
                    if(countError != 0)
                    { 
                        Assert.Fail("Count: " + trackedVehicleCount + ", expected count: " + expectedCount + ", frame: " + i + ", average detections: " + averageDetections);    
                    }
                }
            }
        }
    }
}
