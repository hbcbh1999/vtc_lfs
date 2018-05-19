using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptAssignTest.Framework;
using VTC.Common.RegionConfig;
using VTC.Kernel.Video;

namespace OptAssignTest
{
    [TestClass]
    public class CrossroadTests : ScriptedTestBase
    {
        [TestMethod]
        [Description("Two cars passing the same intersection. One goes vertically, the second one - horizontally.")]
        public void CrossingPaths() // TODO: cars should not go through the intersection at the same time
        {
            var script = CrossingPathScript();

            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;

                if (frame > DetectionThreshold && !script.IsDone(frame))
                {
                    Assert.AreEqual(script.Cars.Count, vehicles.Count, "Both cars should be detected (failed at {0} frame).", frame);
                    // TODO: make sure that each car keeps its direction
                }
            });
        }

        private Script CrossingPathScript()
        {
            var script = new Script();

            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddVerticalPath();

            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddHorizontalPath(Direction.East);

            return script;
        }

        public override IEnumerable<CaptureContext> GetCaptures()
        {
            return new[]
            {
                new CaptureContext(new CaptureEmulator("Crossing paths", CrossingPathScript()), new RegionConfig())
            };
        }
    }
}
