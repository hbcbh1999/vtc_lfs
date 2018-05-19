using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptAssignTest.Framework;
using VTC.Common.RegionConfig;
using VTC.Kernel.Video;

namespace OptAssignTest
{
    [TestClass]
    public class VisibilityLossTests : ScriptedTestBase
    {
        [TestMethod]
        [Description("Car should be tracked after visibility loss until threshold happens.")]
        public void VisibilityLoss_ShouldBeDetected()
        {
            uint frameWhenDetectionLost = (uint) (480 / 2);
            var script = VisibilityLossScript(frameWhenDetectionLost);
            var regionConfig = new RegionConfig();

            RunScript(script, (vista, frame) =>
                {
                    // the car should be detected at that point
                    if (frame == DetectionThreshold)
                    {
                        Assert.AreEqual(script.Cars.Count, vista.CurrentVehicles.Count, "Car still not detected.");
                    }

                    // car should became invisible, but still be tracked 
                    if (frame == frameWhenDetectionLost + DetectionThreshold)
                    {
                        Assert.AreEqual(script.Cars.Count, vista.CurrentVehicles.Count, "Car still should be detected");

                        Assert.IsTrue(vista.CurrentVehicles[0].StateHistory.Last().MissedDetections > 0,
                            "Car visibility loss should be detected.");
                    }

                    // car should not be tracked anymore
                    if (frame == frameWhenDetectionLost + regionConfig.MissThreshold + DetectionThreshold)
                    {
                        Assert.AreEqual(0, vista.CurrentVehicles.Count, "No cars should be detected after certain number of misses");
                    }
                });
        }

        [TestMethod]
        [Description("TrackedObject should be recognized as the same after loss and reappearence within threshold.")]
        public void ReappearenceWithinThreshold_ShouldBeDetected()
        {
            var regionConfig = new RegionConfig();

            uint frameWhenDetectionLost = (uint)(480 / 2);
            var frameWithReappearence = (uint)(frameWhenDetectionLost + regionConfig.MissThreshold - 1);

            var script = ReappearenceWithinThresholdScript(frameWhenDetectionLost, frameWithReappearence);

            RunScript(script, (vista, frame) =>
                {
                    var vehicles = vista.CurrentVehicles;

                    if (frame == DetectionThreshold)
                    {
                        Assert.AreEqual(script.Cars.Count, vehicles.Count, "Car should be detected.");
                    }

                    // car should became invisible, but still be tracked 
                    if (frame == frameWhenDetectionLost + regionConfig.MissThreshold - 2)
                    {
                        Assert.AreEqual(script.Cars.Count, vehicles.Count, "Car still should be detected.");
                        Assert.IsTrue(vehicles[0].StateHistory.Last().MissedDetections > 0, "Car visibility loss should be detected.");
                    }

                    // car should reappear, and should be recognized as already tracked
                    if (frame == frameWithReappearence + DetectionThreshold)
                    {
                        Assert.AreEqual(script.Cars.Count, vehicles.Count, "Car should be detected");
                        Assert.IsTrue(vehicles[0].StateHistory.Count > frameWithReappearence, "It should be the same car as before.");
                        Assert.IsTrue(vehicles[0].StateHistory.Last().MissedDetections == 0, "Car visibility reappearence should be detected.");
                    }
                });
        }

        [TestMethod]
        [Description("TrackedObject should be recognized as a new one after loss and reappearence after threshold.")]
        public void ReappearenceAfterThreshold_ShouldBeDetected()
        {
            var regionConfig = new RegionConfig();
            uint frameWhenDetectionLost = (uint)(480 / 2);

            var frameWithReappearence = (uint)(frameWhenDetectionLost + regionConfig.MissThreshold + 10);

            var script = ReappearenceAfterThresholdScript(frameWhenDetectionLost, frameWithReappearence);

            RunScript(script, (vista, frame) =>
                {
                    var vehicles = vista.CurrentVehicles;

                    if (frame > DetectionThreshold && frame < frameWhenDetectionLost)
                    {
                        Assert.AreEqual(script.Cars.Count, vehicles.Count, "Car should be detected.");
                    }

                    // car should became invisible, but still be tracked 
                    if (frame > frameWhenDetectionLost + DetectionThreshold && frame < frameWhenDetectionLost + regionConfig.MissThreshold)
                    {
                        Assert.AreEqual(script.Cars.Count, vehicles.Count, "Car still should be detected.");

                        Assert.IsTrue(vehicles[0].StateHistory.Last().MissedDetections > 0, "Car visibility loss should be detected.");
                    }

                    // car should reappear
                    if (frame > frameWithReappearence + DetectionThreshold)
                    {
                        Assert.AreEqual(script.Cars.Count, vehicles.Count, "Car should be detected");

                        Assert.IsTrue(vehicles[0].StateHistory.Count < frameWhenDetectionLost, "It should be detected as a new car.");
                        
                    }
                });
        }

        public override IEnumerable<CaptureContext> GetCaptures()
        {
            // ER: dislike that it's calculated in different places (need it because it's reused in RunScript). 
            // Possible source of future errors.
            // think - maybe script should expose it somehow?

            var regionConfig = new RegionConfig();
            uint frameWhenDetectionLost = (uint)(480 / 2);
            var frameWithReappearence = (uint)(frameWhenDetectionLost + regionConfig.MissThreshold - 10);
            var frameWithReappearenceTooLate = (uint)(frameWhenDetectionLost + regionConfig.MissThreshold + 10);

            return new[]
            {
                new CaptureContext(new CaptureEmulator("Visibility loss", VisibilityLossScript(frameWhenDetectionLost)), regionConfig),
                new CaptureContext(new CaptureEmulator("Reappearence (within threshold)", ReappearenceWithinThresholdScript(frameWhenDetectionLost, frameWithReappearence)), regionConfig),
                new CaptureContext(new CaptureEmulator("Reappearence (after threshold)", ReappearenceAfterThresholdScript(frameWhenDetectionLost, frameWithReappearenceTooLate)), regionConfig),
            };
        }

        private Script ReappearenceAfterThresholdScript(uint frameWhenDetectionLost, uint frameWithReappearence)
        {
            var script = new Script();
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddVerticalPath()
                .Visibility(frame => (frame < frameWhenDetectionLost) || (frame > frameWithReappearence));
                // car hidden in the middle
            return script;
        }

        private Script VisibilityLossScript(uint frameWhenDetectionLost)
        {
            var script = new Script();
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddVerticalPath()
                .Visibility(frame => frame < frameWhenDetectionLost); // car visible only in beginning
            return script;
        }

        private Script ReappearenceWithinThresholdScript(uint frameWhenDetectionLost, uint frameWithReappearence)
        {
            var script = new Script();
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddVerticalPath()
                .Visibility(frame => (frame < frameWhenDetectionLost) || (frame >= frameWithReappearence));
            // car hidden in the middle
            return script;
        }
    }
}
