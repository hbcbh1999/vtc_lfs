using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptAssignTest.Framework;
using VTC.Common.RegionConfig;
using VTC.Kernel.Video;

namespace OptAssignTest
{
    [TestClass]
    public class TurnsTests : ScriptedTestBase
    {
        [TestMethod]
        [Description("Left turn should be detected correctly.")]
        public void LeftTurn()
        {
            var script = LeftTurnScript();

            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;

                if (frame > DetectionThreshold && !script.IsDone(frame))
                {
                    Assert.AreEqual(1, vehicles.Count, "Car should be detected all the way.");
                }
            });
        }

        [TestMethod]
        [Description("One car goes straight thru intersection, another turns right.")]
        public void RightAndStraight()
        {
            var script = RightAndTurnScript();

            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;

                if (frame > DetectionThreshold && !script.IsDone(frame))
                {
                    Assert.AreEqual(2, vehicles.Count, "Cars should be detected all the way.");

                    // TODO: validate that exit points are correct
                }
            });
        }

        [TestMethod]
        [Description("Two simultaneous turns should be detected correctly.")]
        public void DualTurns()
        {
            var script = DualTurnScript();

            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;

                if (frame > DetectionThreshold && !script.IsDone(frame))
                {
                    Assert.AreEqual(2, vehicles.Count, "Both cars should be tracked all the way.");

                    // TODO: validate that exit points are correct
                }

            });
        }

        [TestMethod]
        [Description("Two simultaneous turns should be detected correctly, even if one of the vehicles loss detection.")]
        public void DualTurnsWithLossDetection()
        {
            var script = DualTurnWithDetectionLoss();

            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;

                if (frame > DetectionThreshold && !script.IsDone(frame))
                {
                    Assert.AreEqual(2, vehicles.Count, "Both cars should be tracked all the way.");

                    // TODO: validate that exit points are correct
                }
            });
        }

        [TestMethod]
        [Ignore]
        [Description("Two simultaneous turns should be detected correctly, even if both of the vehicles loss detection. Vehicles are in different colors.")]
        public void DualTurnsWithDualLossDetection()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<CaptureContext> GetCaptures()
        {
            return new[]
            {
                new CaptureContext(new CaptureEmulator("Left turn", LeftTurnScript()), new RegionConfig()),
                new CaptureContext(new CaptureEmulator("Right and turn", RightAndTurnScript()), new RegionConfig()),
                new CaptureContext(new CaptureEmulator("Dual turn", DualTurnScript()), new RegionConfig()),
                new CaptureContext(new CaptureEmulator("Dual turn (with detection loss)", DualTurnWithDetectionLoss()), new RegionConfig())
            };
        }

        private Script LeftTurnScript()
        {
            var script = new Script();
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddTurn(Direction.South, Direction.West, new Path.Vector(0, 3*VehicleRadius));
            return script;
        }

        private Script RightAndTurnScript()
        {
            var script = new Script();

            // vehicle enters at bottom and turns right at center
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddTurn(Direction.South, Direction.East, new Path.Vector(5*VehicleRadius + 1, 3*VehicleRadius));

            // vehicle goes from bottom to up
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddVerticalPath(Direction.South, new Path.Vector(-5*VehicleRadius, 3*VehicleRadius));
            return script;
        }


        private Script DualTurnScript()
        {
            var script = new Script();

            // vehicle enters at bottom and turns left at center
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddTurn(Direction.South, Direction.West, new Path.Vector(-3*VehicleRadius, 3*VehicleRadius));

            // vehicle enters at top and turns right at center
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddTurn(Direction.North, Direction.East, new Path.Vector(3*VehicleRadius, -3*VehicleRadius + 1));
            return script;
        }

        private Script DualTurnWithDetectionLoss()
        {
            var script = new Script();

            var expectedFrames = (uint) (640 + 480);
            var expectedTurnFrame = expectedFrames/2;

            // vehicle enters at bottom and turns left at center
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddTurn(Direction.South, Direction.West, new Path.Vector(-3*VehicleRadius, 3*VehicleRadius));

            // vehicle enters at top and turns right at center
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddTurn(Direction.North, Direction.East, new Path.Vector(3*VehicleRadius, -3*VehicleRadius + 1))
                .Visibility(frame => Math.Abs(expectedTurnFrame - frame) > 5); // loss of detection during turn
            return script;
        }
    }
}
